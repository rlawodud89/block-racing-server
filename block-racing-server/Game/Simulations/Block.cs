using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class Block
{
    public BlockType Type { get; }

    public int OwnerId { get; }

    public Block(BlockType type, int ownerId)
    {
        Type = type;
        OwnerId = ownerId;
    }
}