using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Lanes;

public class Car
{
    public int X { get; private set; } = 2;

    public float Speed { get; private set; }

    public float Distance { get; private set; }

    public bool IsStunned { get; private set; }

    public bool IsInvincible { get; private set; }

    public int StunRemainTick { get; private set; }


    public void MoveLeft()
    {
        if (X > 0)
            X--;
    }

    public void MoveRight()
    {
        if (X < Lane.Width - 1)
            X++;
    }

    public void Update(float deltaTime)
    {
        if (IsStunned)
        {
            Distance += Speed * 0.2f * deltaTime;
        }
        else
        {
            Distance += Speed * deltaTime;
        }
    }
}
