using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class Car
{
    public int X { get; set; } = 2;

    public float Speed { get; set; }

    public float Distance { get; set; }

    public bool IsInvincible { get; set; }

    public int StunRemainTick { get; set; }
}
