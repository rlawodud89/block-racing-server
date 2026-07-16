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

    private int _roomId = 0;

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
        return _rooms.TryRemove(id, out _);
    }

    public Room? Find(int id)
    {
        _rooms.TryGetValue(id, out var room);

        return room;
    }

    public IEnumerable<Room> Rooms => _rooms.Values;
}
