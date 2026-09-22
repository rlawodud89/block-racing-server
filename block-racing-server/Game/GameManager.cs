using block_racing_common.Network.Packets;
using block_racing_common.Game.Enums;
using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;
using Microsoft.Extensions.Logging;

namespace block_racing_server.Game;

public class GameManager
{
    public RoomManager _roomManager { get; }
    public MatchMaker _matchMaker { get; }

    private long _currentTick;
    public long CurrentTick => _currentTick;

    private readonly ILogger<GameManager> _logger;


    public GameManager(RoomManager roomManager, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GameManager>();

        _roomManager = roomManager;
        _matchMaker = new MatchMaker(roomManager, loggerFactory);
    }

    public async Task Update()
    {
        _currentTick++;

        await _matchMaker.TryMatch();

        var rooms = _roomManager.Rooms.ToList();

        await Task.WhenAll(
            rooms.Select(room => room.Update(_currentTick))
        );

        foreach (Room room in rooms)
        {
            if (room.State == RoomState.Closing)
            {
                _logger.LogInformation(
                    "Removing closed room. RoomId={RoomId}",
                    room.Id);

                _roomManager.RemoveRoom(room.Id);
            }
        }
    }

    public void RegisterPlayer(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Player registration ignored because player is null.");

            return;
        }

        if (player.Room != null)
        {
            _logger.LogWarning(
                "Player registration ignored because player is already in a room. PlayerId={PlayerId} RoomId={RoomId}",
                player.Id,
                player.Room.Id);

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            _logger.LogWarning(
                "Player registration ignored because player is already in matchmaking. PlayerId={PlayerId} MatchState={MatchState}",
                player.Id,
                player.MatchState);

            return;
        }

        _matchMaker.Register(player);

        _logger.LogInformation(
            "Player registered. PlayerId={PlayerId}",
            player.Id);
    }

    public async Task UnregisterPlayer(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Player unregistration ignored because player is null.");

            return;
        }

        _logger.LogInformation(
            "Unregistering player. PlayerId={PlayerId}",
            player.Id);

        var room = player.Room;

        if (room != null)
        {
            _logger.LogInformation(
                "Removing player from room before unregistering. PlayerId={PlayerId} RoomId={RoomId}",
                player.Id,
                room.Id);

            await room.RemovePlayerAsync(player);
        }

        _matchMaker.Unregister(player);

        _logger.LogInformation(
            "Player unregistered. PlayerId={PlayerId}",
            player.Id);
    }

    public void EnqueueMatch(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Player enqueue ignored because player is null.");

            return;
        }

        _logger.LogInformation(
            "Enqueuing player for matchmaking. PlayerId={PlayerId}",
            player.Id);

        _matchMaker.Enqueue(player);
    }

    public void CancelMatch(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Player cancel match ignored because player is null.");

            return;
        }

        _logger.LogInformation(
            "Cancelling player matchmaking. PlayerId={PlayerId}",
            player.Id);

        _matchMaker.Cancel(player);
    }

    public async Task CreatePrivateRoom(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Create room request ignored because player is null.");

            return;
        }

        _logger.LogInformation(
            "Creating private room. PlayerId={PlayerId}",
            player.Id);

        if (player.Room != null)
        {
            _logger.LogWarning(
                "Create room request rejected because player is already in a room. PlayerId={PlayerId} RoomId={RoomId}",
                player.Id,
                player.Room.Id);

            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.AlreadyInRoom
                });

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            //_logger.LogWarning(
            //    "Create room request rejected because player is already queued. PlayerId={PlayerId} MatchState={MatchState}",
            //    player.Id,
            //    player.MatchState);

            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.AlreadyQueued
                });

            return;
        }


        Room? room = _roomManager.CreatePrivateRoom();

        if (room == null)
        {
            _logger.LogError(
                "Failed to create private room. PlayerId={PlayerId}",
                player.Id);

            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.UnknownError
                });

            return;
        }

        _logger.LogInformation(
            "Private room created. RoomId={RoomId} RoomCode={RoomCode} PlayerId={PlayerId}",
            room.Id,
            room.Code,
            player.Id);

        bool added = await room.AddPlayer(player);

        if (!added)
        {
            _logger.LogError(
                "Failed to add player to newly created private room. PlayerId={PlayerId} RoomId={RoomId}",
                player.Id,
                room.Id);

            _roomManager.RemoveRoom(room.Id);

            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.UnknownError
                });

            return;
        }

        _logger.LogInformation(
            "Player added to private room. PlayerId={PlayerId} RoomId={RoomId}",
            player.Id,
            room.Id);


        await player.Session.SendAsync(
            new S_RoomCreatedPacket
            {
                Result = RoomCreateResult.Success,
                RoomId = room.Id,
                RoomCode = room.Code
            });
    }

    public async Task JoinRoom(Player player, string roomCode)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Join room request ignored because player is null.");

            return;
        }

        //_logger.LogInformation(
        //    "Join room request received. PlayerId={PlayerId} RoomCode={RoomCode}",
        //    player.Id,
        //    roomCode);

        if (player.Room != null)
        {
            //_logger.LogWarning(
            //    "Join room request rejected because player is already in a room. PlayerId={PlayerId} RoomId={RoomId}",
            //    player.Id,
            //    player.Room.Id);

            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.AlreadyInRoom
                });

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            //_logger.LogWarning(
            //    "Join room request rejected because player is already queued. PlayerId={PlayerId} MatchState={MatchState}",
            //    player.Id,
            //    player.MatchState);

            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.AlreadyQueued
                });

            return;
        }


        roomCode = roomCode.Trim().ToUpperInvariant();

        if(roomCode.Length != 6)
        {
            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.RoomNotFound,
                });
            return;
        }

        Room? room = _roomManager.Find(roomCode);

        if (room == null)
        {
            //_logger.LogWarning(
            //    "Join room request rejected because room was not found. PlayerId={PlayerId} RoomCode={RoomCode}",
            //    player.Id,
            //    roomCode);

            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.RoomNotFound,
                });

            return;
        }

        bool added = await room.AddPlayer(player);

        if (!added)
        {
            _logger.LogWarning(
                "Failed to add player to room. Room may be full. PlayerId={PlayerId} RoomId={RoomId}",
                player.Id,
                room.Id);

            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.RoomFull,
                });

            return;
        }


        _logger.LogInformation(
            "Player joined room. PlayerId={PlayerId} RoomId={RoomId}",
            player.Id,
            room.Id);

        await player.Session.SendAsync(
            new S_RoomJoinedPacket
            {
                Result = RoomJoinResult.Success,
                RoomId = room.Id
            });
    }

    public async Task LeaveRoom(Player player)
    {
        if (player == null)
        {
            _logger.LogWarning(
                "Leave room request ignored because player is null.");
            return;
        }
        if (player.Room == null)
        {
            _logger.LogWarning(
                "Leave room request ignored because player is not in a room. PlayerId={PlayerId}",
                player.Id);
            return;
        }

        _logger.LogInformation(
            "Player leaving room. PlayerId={PlayerId} RoomId={RoomId}",
            player.Id,
            player.Room.Id);

        await player.Room.RemovePlayerAsync(player);
    }
}
