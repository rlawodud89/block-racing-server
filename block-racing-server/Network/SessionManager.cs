using block_racing_common.Network;
using System.Collections.Concurrent;

namespace block_racing_server.Network;

public class SessionManager
{
    private readonly ConcurrentDictionary<int, PlayerSession> _sessions
        = new();

    private int _idGenerator = 0;

    public int Count => _sessions.Count;

    public IReadOnlyCollection<PlayerSession> Sessions
        => _sessions.Values.ToArray();


    public int Add(PlayerSession session)
    {
        int id = Interlocked.Increment(ref _idGenerator);

        session.Id = id;

        _sessions.TryAdd(id, session);

        Console.WriteLine($"Session Add : {id}");

        return id;
    }

    public void Remove(PlayerSession session)
    {
        _sessions.TryRemove(session.Id, out _);

        Console.WriteLine($"Session Remove : {session.Id}");
    }

    public PlayerSession? Find(int id)
    {
        _sessions.TryGetValue(id, out var session);

        return session;
    }

    public async Task BroadcastAsync(byte[] data)
    {
        foreach (var session in _sessions.Values)
        {
            await session.SendAsync(data);
        }
    }
}