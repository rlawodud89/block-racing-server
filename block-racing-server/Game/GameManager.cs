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

        await Task.WhenAll(
            _roomManager.Rooms
            .Select(room => room.Update())
        );
    }

    public void RegisterPlayer(Player player)
    {
        if (player == null ||
            player.Room != null ||
            player.MatchState != MatchState.None)
            return;

        _matchMaker.Register(player);
    }

    public void UnregisterPlayer(Player player)
    {
        if (player == null) return;

        player.MatchState = MatchState.None; // 먼저 상태 차단

        _matchMaker.Unregister(player);

        var room = player.Room;
        player.Room = null;

        room?.RemovePlayer(player);
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
