using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Players;

public class PlayerInputCommand
{
    public Player Player { get; }

    public InputType Type { get; }

    public PlayerInputCommand(Player player,InputType type)
    {
        Player = player;
        Type = type;
    }
}
