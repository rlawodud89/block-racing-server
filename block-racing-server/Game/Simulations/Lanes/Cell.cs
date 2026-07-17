using block_racing_server.Game.Simulations.Blocks;

namespace block_racing_server.Game.Simulations.Lanes;

public class Cell
{
    public Block? Block { get; set; }

    public bool IsEmpty => Block == null;
}
