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

    public async Task TryMatch()
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

    private bool TryReserve(Player p)
    {
        if (p.MatchState != MatchState.Queued)
            return false;

        p.MatchState = MatchState.Matching;
        return true;
    }
}
