using block_racing_server.Game.Players;

namespace block_racing_server.Game.Simulations;

public class GameState
{
    public long Tick { get; private set; }

    public float ElapsedTime { get; private set; }

    public int TargetDistance { get; } = 50;

    public bool IsGameEnd { get; private set; } = false;

    private readonly Dictionary<int, Player> _players = new();


    public IReadOnlyDictionary<int, Player> Players
        => _players;


    public void AddPlayer(Player player)
    {
        _players.Add(player.Id, player);
    }


    public void SetTick(long currentTick)
    {
        Tick = currentTick;
    }

    public void UpdateTick(long currentTick, float deltaTime)
    {
        Tick = currentTick;
        ElapsedTime += deltaTime;
    }


    public void EndGame()
    {
        IsGameEnd = true;
    }

}