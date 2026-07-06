using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;

namespace block_racing_server.Game;

public class GameManager
{
    public RoomManager RoomManager { get; }
    public MatchMaker MatchMaker { get; }

    private readonly List<Player> _players = new();

    public GameManager(RoomManager roomManager)
    {
        RoomManager = roomManager;
        MatchMaker = new MatchMaker(roomManager);
    }

    public void Update()
    {
        MatchMaker.TryMatch();
    }

    public void RegisterPlayer(Player player)
    {
        _players.Add(player);
        MatchMaker.Register(player);
    }

    public void UnregisterPlayer(Player player)
    {
        _players.Remove(player);
    }

    public void EnqueueMatch(Player player)
    {
        MatchMaker.Enqueue(player);
    }

    public void CancleMatch(Player player)
    {
        MatchMaker.Cancel(player);
    }
}
