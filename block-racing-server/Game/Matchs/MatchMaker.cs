using block_racing_common.Network.Packets;
using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

public class MatchMaker
{
    private readonly ConcurrentDictionary<long, Player> _players = new();

    private readonly RoomManager _roomManager;
    private readonly ILogger<MatchMaker> _logger;

    public MatchMaker(RoomManager roomManager, ILoggerFactory loggerFactory)
    {
        _roomManager = roomManager;
        _logger = loggerFactory.CreateLogger<MatchMaker>();
    }

    public void Register(Player player)
    {
        player.MatchState = MatchState.None;

        _players[player.Id] = player;

        _logger.LogInformation(
             "Player registered to matchmaking. PlayerId={PlayerId} MatchState={MatchState} PlayerCount={PlayerCount}",
             player.Id,
             player.MatchState,
             _players.Count);
    }

    public void Unregister(Player player)
    {
        _logger.LogInformation(
            "Unregistering player from matchmaking. PlayerId={PlayerId}",
            player.Id);

        player.MatchState = MatchState.None;

        if (_players.TryGetValue(player.Id, out var currentPlayer) &&
            ReferenceEquals(currentPlayer, player))
        {
            _players.TryRemove(player.Id, out _);

            _logger.LogInformation(
                "Player unregistered from matchmaking. PlayerId={PlayerId} PlayerCount={PlayerCount}",
                player.Id,
                _players.Count);
        }
        else
        {
            _logger.LogWarning(
                "Failed to unregister player. PlayerId={PlayerId}",
                player.Id);
        }
    }

    public void Enqueue(Player player)
    {
        _logger.LogDebug(
            "Match enqueue requested. PlayerId={PlayerId} MatchState={MatchState} Registered={Registered}",
            player.Id,
            player.MatchState,
            _players.ContainsKey(player.Id));

        if (!_players.TryGetValue(player.Id, out var registeredPlayer))
        {
            _logger.LogWarning(
               "Match enqueue failed because player is not registered. PlayerId={PlayerId}",
               player.Id);

            return;
        }

        if (!ReferenceEquals(registeredPlayer, player))
        {
            _logger.LogWarning(
                 "Match enqueue failed because player reference does not match. PlayerId={PlayerId}",
                 player.Id);

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            _logger.LogWarning(
                "Match enqueue ignored because player is not in None state. PlayerId={PlayerId} MatchState={MatchState}",
                player.Id,
                player.MatchState);

            return;
        }


        player.MatchState = MatchState.Queued;

        _logger.LogInformation(
            "Player entered matchmaking queue. PlayerId={PlayerId} MatchState={MatchState}",
            player.Id,
            player.MatchState);
    }

    public void Cancel(Player player)
    {
        if (player.MatchState == MatchState.InRoom)
        {
            _logger.LogWarning(
                "Match cancellation ignored because player is already in a room. PlayerId={PlayerId}",
                player.Id);

            return;
        }


        player.MatchState = MatchState.None;

        _logger.LogInformation(
           "Player left matchmaking queue. PlayerId={PlayerId}",
           player.Id);
    }

    public async Task TryMatch()
    {
        var stopwatch = Stopwatch.StartNew();

        // Candidate
        var candidateStart = stopwatch.Elapsed;

        var candidates = _players.Values
            .Where(p => p.MatchState == MatchState.Queued)
            .Take(2)
            .ToList();

        var candidateElapsed =
            stopwatch.Elapsed - candidateStart;

        if (candidates.Count < 2)
            return;

        var p1 = candidates[0];
        var p2 = candidates[1];

        // Reserve
        var reserveStart = stopwatch.Elapsed;

        if (!TryReserve(p1))
            return;

        if (!TryReserve(p2))
        {
            p1.MatchState = MatchState.Queued;
            return;
        }

        var reserveElapsed =
            stopwatch.Elapsed - reserveStart;

        // Room Create
        var roomStart = stopwatch.Elapsed;

        var room = _roomManager.CreateRoom();

        var roomCreateElapsed =
            stopwatch.Elapsed - roomStart;

        // Add Player 1
        var addP1Start = Stopwatch.GetTimestamp();

        bool addedP1 = await room.AddPlayer(p1);

        var addP1Elapsed =
            Stopwatch.GetElapsedTime(addP1Start);

        // Add Player 2
        var addP2Start = Stopwatch.GetTimestamp();

        bool addedP2 = await room.AddPlayer(p2);

        var addP2Elapsed =
            Stopwatch.GetElapsedTime(addP2Start);

        stopwatch.Stop();

        // 느린 AddP2만 별도로 기록
        if (addP2Elapsed > TimeSpan.FromMilliseconds(30))
        {
            _logger.LogWarning(
                "AddP2 slow. " +
                "RoomId={RoomId} " +
                "PlayerId={PlayerId} " +
                "ElapsedMs={ElapsedMs:F2}",
                room.Id,
                p2.Id,
                addP2Elapsed.TotalMilliseconds);
        }

        _logger.LogWarning(
            "Match timing. " +
            "CandidateMs={CandidateMs:F2} " +
            "ReserveMs={ReserveMs:F2} " +
            "RoomCreateMs={RoomCreateMs:F2} " +
            "AddP1Ms={AddP1Ms:F2} " +
            "AddP2Ms={AddP2Ms:F2} " +
            "TotalMs={TotalMs:F2} " +
            "RoomId={RoomId}",
            candidateElapsed.TotalMilliseconds,
            reserveElapsed.TotalMilliseconds,
            roomCreateElapsed.TotalMilliseconds,
            addP1Elapsed.TotalMilliseconds,
            addP2Elapsed.TotalMilliseconds,
            stopwatch.Elapsed.TotalMilliseconds,
            room.Id);

        // 실패 처리
        if (!addedP1 || !addedP2)
        {
            _logger.LogError(
                "Failed to add matched players to room. " +
                "RoomId={RoomId} PlayerA={PlayerA} PlayerB={PlayerB} " +
                "AddedPlayerA={AddedPlayerA} AddedPlayerB={AddedPlayerB}",
                room.Id,
                p1.Id,
                p2.Id,
                addedP1,
                addedP2);

            _roomManager.RemoveRoom(room.Id);

            p1.Room = null;
            p2.Room = null;

            p1.MatchState = MatchState.None;
            p2.MatchState = MatchState.None;
        }
    }

    private bool TryReserve(Player player)
    {
        if (player.MatchState != MatchState.Queued)
            return false;

        player.MatchState = MatchState.Matching;

        _logger.LogDebug(
           "Player reserved for matchmaking. PlayerId={PlayerId}",
           player.Id);

        return true;
    }
}