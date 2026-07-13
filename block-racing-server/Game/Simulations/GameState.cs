using block_racing_server.Game.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class GameState
{
    public long Tick { get; private set; }

    public float ElapsedTime { get; private set; }

    public int TargetDistance { get; } = 500;

    public bool IsGameEnd { get; private set; }


    private readonly PieceGenerator _pieceGenerator = new();

    private readonly Dictionary<int, Player> _players = new();


    public IReadOnlyDictionary<int, Player> Players
        => _players;


    public void AddPlayer(Player player)
    {
        _players.Add(player.Id, player);
    }


    public void UpdateTick(float deltaTime)
    {
        Tick++;
        ElapsedTime += deltaTime;
    }


    public void EndGame()
    {
        IsGameEnd = true;
    }

    public BlockPiece CreatePiece()
    {
        return _pieceGenerator.Create();
    }
}