using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
