using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Players;

public class PlayerInput
{
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Mode { get; set; }
    public bool Drop { get; set; }

    public void Clear()
    {
        Left = false;
        Right = false;
        Mode = false;
        Drop = false;
    }
}
