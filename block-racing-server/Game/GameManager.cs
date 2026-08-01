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
            if (room.State == RoomState.Ended)
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
}
