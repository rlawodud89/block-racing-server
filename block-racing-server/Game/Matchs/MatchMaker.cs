using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;
using System.Collections.Concurrent;
using System.Linq;

namespace block_racing_server.Game.Matchs;

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
        _players.TryAdd(player.Id, player);
    }

    public void Unregister(Player player)
    {
        player.MatchState = MatchState.None;

        _players.TryRemove(player.Id, out _);
    }

    public void Enqueue(Player player)
    {
        if (player.MatchState != MatchState.None)
            return;

        player.MatchState = MatchState.Queued;
    }

    public void Cancel(Player player)
    {
        if (player.MatchState == MatchState.InRoom)
            return;

        player.MatchState = MatchState.None;
    }

    public async void TryMatch()
    {
        var candidates = _players.Values
            .Where(p => p.MatchState == MatchState.Queued)
            .Take(2)
            .ToList();

        if (candidates.Count < 2)
            return;

        var p1 = candidates[0];
        var p2 = candidates[1];

        // 원자적 상태 변경
        if (!TryReserve(p1) || !TryReserve(p2))
            return;

        var room = _roomManager.CreateRoom();

        await room.AddPlayer(p1);
        await room.AddPlayer(p2);

        p1.MatchState = MatchState.InRoom;
        p2.MatchState = MatchState.InRoom;
    }

    private bool TryReserve(Player p)
    {
        if (p.MatchState != MatchState.Queued)
            return false;

        p.MatchState = MatchState.Matching;
        return true;
    }
}
