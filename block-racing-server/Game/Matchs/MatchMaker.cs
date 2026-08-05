using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;
using System.Collections.Concurrent;

public class MatchMaker
{
    private readonly ConcurrentDictionary<int, Player> _players = new();

    private readonly RoomManager _roomManager;

    public MatchMaker(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void Register(Player player)
    {
        player.MatchState = MatchState.None;

        _players[player.Id] = player;

        Console.WriteLine(
        $"[MATCH] REGISTER Player={player.Id}, " +
        $"State={player.MatchState}, " +
        $"Count={_players.Count}");
    }

    public void Unregister(Player player)
    {
        Console.WriteLine(
            $"[MATCH] UNREGISTER Player={player.Id}");

        player.MatchState = MatchState.None;

        if (_players.TryGetValue(player.Id, out var currentPlayer) &&
            ReferenceEquals(currentPlayer, player))
        {
            _players.TryRemove(player.Id, out _);
        }
    }

    public void Enqueue(Player player)
    {
        Console.WriteLine(
            $"[MATCH] ENQUEUE REQUEST Player={player.Id}, " +
            $"State={player.MatchState}, " +
            $"Registered={_players.ContainsKey(player.Id)}");

        if (!_players.TryGetValue(player.Id, out var registeredPlayer))
        {
            Console.WriteLine(
                $"[MATCH] ENQUEUE FAIL - NOT REGISTERED Player={player.Id}");

            return;
        }

        if (!ReferenceEquals(registeredPlayer, player))
        {
            Console.WriteLine(
                $"[MATCH] ENQUEUE FAIL - REFERENCE MISMATCH Player={player.Id}");

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            Console.WriteLine(
                $"[MATCH] ENQUEUE IGNORE - State={player.MatchState}");

            return;
        }

        player.MatchState = MatchState.Queued;

        Console.WriteLine(
            $"[MATCH] ENQUEUE SUCCESS Player={player.Id}");
    }

    public void Cancel(Player player)
    {
        if (player.MatchState == MatchState.InRoom)
            return;

        player.MatchState = MatchState.None;
    }

    public async Task TryMatch()
    {
        //Console.WriteLine(
        //$"[MATCH] CHECK " +
        //$"{string.Join(", ", _players.Values.Select(
        //    p => $"{p.Id}:{p.MatchState}"))}");

        var candidates = _players.Values
            .Where(p => p.MatchState == MatchState.Queued)
            .Take(2)
            .ToList();

        if (candidates.Count < 2)
            return;

        Console.WriteLine(
        $"[MATCH] FOUND {candidates[0].Id} vs {candidates[1].Id}");

        var p1 = candidates[0];
        var p2 = candidates[1];

        if (!TryReserve(p1))
            return;

        if (!TryReserve(p2))
        {
            p1.MatchState = MatchState.Queued;
            return;
        }

        var room = _roomManager.CreateRoom();

        bool addedP1 = await room.AddPlayer(p1);
        bool addedP2 = await room.AddPlayer(p2);

        if (!addedP1 || !addedP2)
        {
            _roomManager.RemoveRoom(room.Id);

            p1.Room = null;
            p2.Room = null;

            p1.MatchState = MatchState.None;
            p2.MatchState = MatchState.None;

            return;
        }

        p1.MatchState = MatchState.InRoom;
        p2.MatchState = MatchState.InRoom;
    }

    private bool TryReserve(Player player)
    {
        if (player.MatchState != MatchState.Queued)
            return false;

        player.MatchState = MatchState.Matching;
        return true;
    }
}