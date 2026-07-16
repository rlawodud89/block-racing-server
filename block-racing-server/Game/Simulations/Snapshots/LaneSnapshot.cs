using block_racing_server.Game.Simulations.Lanes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Snapshots;


public class LaneSnapshot
{
    public int Width { get; }

    public int Height { get; }


    public byte[] Blocks { get; }

    public List<BlockSnapshot> FlyingBlocks { get; }


    public LaneSnapshot(Lane lane)
    {
        Width = Lane.Width;
        Height = Lane.Height;

        Blocks = new byte[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index =
                    y * Width + x;


                Blocks[index] =
                    lane.Grid[y, x].Block == null
                    ? (byte)0 // 빈칸
                    : (byte)1; // 블록
            }
        }
    }

    public LaneSnapshot(byte[] blocks)
    {
        Blocks = blocks;
    }
}
