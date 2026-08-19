using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Rooms;

public class RoomManager
{
    private readonly ConcurrentDictionary<int, Room> _rooms = new();

    private readonly ConcurrentDictionary<string, int> _roomCodes = new();

    private int _roomId = 0;

    private const string RoomCodeChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public Room CreateRoom()
    {
        int id = Interlocked.Increment(ref _roomId);

        var room = new Room(id);

        _rooms.TryAdd(id, room);

        Console.WriteLine($"Room {id} created.");

        return room;
    }

    public bool RemoveRoom(int id)
    {
        if (!_rooms.TryRemove(id, out var room))
            return false;

        foreach (var pair in _roomCodes)
        {
            if (pair.Value == id)
            {
                _roomCodes.TryRemove(pair.Key, out _);
                break;
            }
        }

        return true;
    }

    public Room? Find(int id)
    {
        _rooms.TryGetValue(id, out var room);

        return room;
    }

    public Room? Find(string roomCode)
    {
        if (!_roomCodes.TryGetValue(roomCode, out int roomId))
            return null;

        return Find(roomId);
    }

    public IEnumerable<Room> Rooms => _rooms.Values;

    public string RegisterRoomCode(Room room)
    {
        while (true)
        {
            string roomCode = GenerateRoomCode();

            if (_roomCodes.TryAdd(roomCode, room.Id))
                return roomCode;
        }
    }

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
