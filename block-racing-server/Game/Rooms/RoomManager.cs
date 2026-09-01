using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Rooms;

public class RoomManager
{
    private readonly ConcurrentDictionary<long, Room> _rooms = new();

    private readonly ConcurrentDictionary<string, long> _roomCodes = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RoomManager> _logger;

    private long _roomId = 0;

    private const string RoomCodeChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public RoomManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<RoomManager>();
    }

    public Room CreateRoom()
    {
        long id = Interlocked.Increment(ref _roomId);

        var room = new Room(id, _loggerFactory);

        _rooms.TryAdd(id, room);

        _logger.LogInformation(
            "Room created. RoomId={RoomId} RoomCount={RoomCount}",
            id,
            _rooms.Count);

        return room;
    }

    public Room? CreatePrivateRoom()
    {
        long id = Interlocked.Increment(ref _roomId);

        while (true)
        {
            string roomCode = GenerateRoomCode();

            if (!_roomCodes.TryAdd(roomCode, id))
            {
                _logger.LogDebug(
                    "Room code collision detected. RoomCode={RoomCode}",
                    roomCode);

                continue;
            }


            var room = new Room(id, _loggerFactory, roomCode);

            if (!_rooms.TryAdd(id, room))
            {
                _roomCodes.TryRemove(roomCode, out _);

                _logger.LogError(
                    "Failed to add private room. RoomId={RoomId} RoomCode={RoomCode}",
                    id,
                    roomCode);

                return null;
            }


            _logger.LogInformation(
                "Private room created. RoomId={RoomId} RoomCode={RoomCode} RoomCount={RoomCount}",
                id,
                roomCode,
                _rooms.Count);

            return room;
        }
    }

    public bool RemoveRoom(long id)
    {
        if (!_rooms.TryRemove(id, out var room))
        {
            _logger.LogWarning(
                "Failed to remove room because room was not found. RoomId={RoomId}",
                id);

            return false;
        }

        if (room.Code != null)
        {
            _roomCodes.TryRemove(room.Code, out _);
        }

        _logger.LogInformation(
            "Room removed. RoomId={RoomId} RoomCode={RoomCode} RoomCount={RoomCount}",
            id,
            room.Code,
            _rooms.Count);

        return true;
    }

    public Room? Find(long id)
    {
        _rooms.TryGetValue(id, out var room);

        return room;
    }

    public Room? Find(string roomCode)
    {
        if (!_roomCodes.TryGetValue(roomCode, out long roomId))
        {
            _logger.LogDebug(
                "Room not found by code. RoomCode={RoomCode}",
                roomCode);

            return null;
        }

        return Find(roomId);
    }

    public IEnumerable<Room> Rooms => _rooms.Values;


    private string GenerateRoomCode()
    {
        Span<char> chars = stackalloc char[6];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] =
                RoomCodeChars[Random.Shared.Next(RoomCodeChars.Length)];
        }

        return new string(chars);
    }

}
