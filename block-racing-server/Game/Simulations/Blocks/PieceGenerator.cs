using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Simulations.Blocks;

public class PieceGenerator
{
    private Random random = new();


    public BlockPiece Create()
    {
        PieceType type =
            (PieceType)random.Next(
                Enum.GetValues<PieceType>().Length
            );


        return new BlockPiece(type);
    }
}
