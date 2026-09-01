using block_racing_common.Network;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace block_racing_server.Network;

public class SessionManager
{
    private readonly ConcurrentDictionary<long, PlayerSession> _sessions
        = new();

    private long _idGenerator = 0;

    private readonly ILogger<SessionManager> _logger;

    public int Count => _sessions.Count;

    public IReadOnlyCollection<PlayerSession> Sessions
        => _sessions.Values.ToArray();

    public SessionManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SessionManager>();
    }


    public long Add(PlayerSession session)
    {
        long id = Interlocked.Increment(ref _idGenerator);

        session.Id = id;

        _sessions.TryAdd(id, session);

        _logger.LogInformation(
            "Session added. SessionId={SessionId} SessionCount={SessionCount}",
            id,
            _sessions.Count);

        return id;
    }

    public void Remove(PlayerSession session)
    {
        _sessions.TryRemove(session.Id, out _);

        _logger.LogInformation(
            "Session removed. SessionId={SessionId} SessionCount={SessionCount}",
            session.Id,
            _sessions.Count);
    }

    public PlayerSession? Find(long id)
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

    public async Task UpdateAsync()
    {
        foreach (var session in _sessions.Values)
        {
            if (session.IsHeartbeatTimeout())
            {
                _logger.LogWarning(
                    "Heartbeat timeout. SessionId={SessionId}",
                    session.Id);

                await session.DisconnectAsync();

                continue;
            }

            if (session.ShouldSendHeartbeat())
            {
                await session.SendHeartbeatAsync();
            }
        }
    }
}