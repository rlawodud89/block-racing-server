using block_racing_server.Game.Players;
using block_racing_server.Game.Matchs;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public int Id { get; }

    private readonly List<Player> _players = new();

    public bool IsStarted { get; private set; }

    public Room(int id)
    {
        Id = id;
    }

    public IReadOnlyList<Player> Players => _players;

    public bool AddPlayer(Player player)
    {
        if (_players.Count >= 2)
            return false;

        _players.Add(player);
        player.Room = this;

        if (_players.Count == 2)
        {
            StartGame();
        }

        return true;
    }

    public bool RemovePlayer(Player player)
    {
        if (!_players.Remove(player))
            return false;

        player.Room = null;
        player.MatchState = MatchState.None;

        Console.WriteLine($"Room {Id}: Player removed");

        // 방 종료 처리
        if (_players.Count == 0)
        {
            IsStarted = false;
            Console.WriteLine($"Room {Id} closed (empty)");
        }

        return true;
    }

    private void StartGame()
    {
        IsStarted = true;

        Console.WriteLine($"Room {Id} Game Start!");
    }

    public void Update()
    {
        
    }
}