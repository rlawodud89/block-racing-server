
using block_racing_server.Data;

namespace block_racing_server.Game.Simulations.Lanes;

public class Car
{
    public const int Width = 1;
    public const int Height = 2;

    public int X { get; private set; } = 2;

    public float Speed { get; private set; }

    public float CurrentSpeed
    {
        get
        {
            return IsStunned
                ? Speed * GameBalance.StunnedSpeedMultiplier
                : Speed;
        }
    }

    public float Distance { get; private set; }

    public bool IsStunned { get; private set; }

    public bool IsInvincible { get; private set; }

    public int StunRemainTick { get; private set; }


    public Car()
    {
        Speed = GameBalance.InitialCarSpeed;
    }

    public void Reset()
    {
        X = 2;
        Speed = GameBalance.InitialCarSpeed;
        Distance = 0f;
        IsStunned = false;
        IsInvincible = false;
        StunRemainTick = 0;
    }

    public void MoveLeft()
    {
        if (X > 0)
            X--;
    }

    public void MoveRight()
    {
        if (X + Width < Lane.Width)
            X++;
    }

    public void Update(float deltaTime)
    {
        Distance += CurrentSpeed * deltaTime;

        if (!IsStunned)
            return;

        StunRemainTick--;

        if (StunRemainTick > 0)
            return;

        IsStunned = false;
        IsInvincible = false;

        Speed -= GameBalance.CarSpeedPenalty;

        if (Speed < GameBalance.InitialCarSpeed)
            Speed = GameBalance.InitialCarSpeed;
    }

    public void OnCollision()
    {
        if (IsInvincible)
            return;

        IsStunned = true;
        IsInvincible = true;

        StunRemainTick = GameBalance.CarStunDurationTick;
    }

    public void AddLineClearSpeed(int lineCount)
    {
        Speed = MathF.Min(
            Speed + lineCount * GameBalance.LineClearSpeedBonus,
            GameBalance.CarMaxSpeed
        );
    }
}
