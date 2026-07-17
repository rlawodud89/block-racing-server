using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Simulations.Blocks;

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