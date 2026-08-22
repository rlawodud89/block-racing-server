using block_racing_common.Network.Packets;
using block_racing_common.Game.Enums;
using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;

namespace block_racing_server.Game;

public class GameManager
{
    public RoomManager _roomManager { get; }
    public MatchMaker _matchMaker { get; }

    public GameManager(RoomManager roomManager)
    {
        _roomManager = roomManager;
        _matchMaker = new MatchMaker(roomManager);
    }

    public async Task Update()
    {
        await _matchMaker.TryMatch();

        var rooms = _roomManager.Rooms.ToList();

        await Task.WhenAll(
            rooms.Select(room => room.Update())
        );

        foreach (Room room in rooms)
        {
            if (room.State == RoomState.Closing)
            {
                _roomManager.RemoveRoom(room.Id);
            }
        }
    }

    public void RegisterPlayer(Player player)
    {
        if (player == null ||
            player.Room != null ||
            player.MatchState != MatchState.None)
            return;

        _matchMaker.Register(player);
    }

    public async Task UnregisterPlayer(Player player)
    {
        if (player == null)
            return;

        var room = player.Room;

        if (room != null)
        {
            await room.RemovePlayerAsync(player);
        }

        _matchMaker.Unregister(player);
    }

    public void EnqueueMatch(Player player)
    {
        _matchMaker.Enqueue(player);
    }

    public void CancelMatch(Player player)
    {
        _matchMaker.Cancel(player);
    }

    public async Task CreateRoom(Player player)
    {
        if (player == null)
            return;

        if (player.Room != null)
        {
            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.AlreadyInRoom
                });

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.AlreadyQueued
                });

            return;
        }


        Room room = _roomManager.CreateRoom();

        string roomCode = _roomManager.RegisterRoomCode(room);

        bool added = await room.AddPlayer(player);

        if (!added)
        {
            _roomManager.RemoveRoom(room.Id);

            await player.Session.SendAsync(
                new S_RoomCreatedPacket
                {
                    Result = RoomCreateResult.UnknownError
                });

            return;
        }


        await player.Session.SendAsync(
            new S_RoomCreatedPacket
            {
                Result = RoomCreateResult.Success,
                RoomId = room.Id,
                RoomCode = roomCode
            });
    }

    public async Task JoinRoom(Player player, string roomCode)
    {
        if (player == null)
            return;

        if (player.Room != null)
        {
            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.AlreadyInRoom
                });

            return;
        }

        if (player.MatchState != MatchState.None)
        {
            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.AlreadyQueued
                });

            return;
        }


        roomCode = roomCode.Trim().ToUpperInvariant();

        Room? room = _roomManager.Find(roomCode);

        if (room == null)
        {
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
            await player.Session.SendAsync(
                new S_RoomJoinedPacket
                {
                    Result = RoomJoinResult.RoomFull,
                });

            return;
        }


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
            return;

        if (player.Room == null)
            return;

        await player.Room.RemovePlayerAsync(player);
    }
}
