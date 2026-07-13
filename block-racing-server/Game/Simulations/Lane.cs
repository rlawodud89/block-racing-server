using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class Lane
{
    public const int Width = 5;
    public const int Height = 20;

    public int PlayerId { get; }

    // 게임판
    public Cell[,] Grid { get; }

    // 차
    public Car Car { get; }

    // 현재 들고 있는 블록
    public BlockPiece CurrentPiece { get; set; }

    // 상대에게서 날아오는 공격
    public Queue<AttackPiece> PendingAttacks { get; }

    public Lane(int playerId)
    {
        PlayerId = playerId;

        Grid = new Cell[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Grid[y, x] = new Cell();
            }
        }

        Car = new Car();
        PendingAttacks = new Queue<AttackPiece>();
    }
}
