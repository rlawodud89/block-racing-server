

namespace block_racing_server.Game.Simulations.Lanes;

public class Car
{
    public const int Width = 2;
    public const int Height = 1;

    public int X { get; private set; } = 2;

    public float Speed { get; private set; }

    public float CurrentSpeed
    {
        get
        {
            return IsStunned
                ? Speed * 0.2f
                : Speed;
        }
    }

    public float Distance { get; private set; }

    public bool IsStunned { get; private set; }

    public bool IsInvincible { get; private set; }

    public int StunRemainTick { get; private set; }

    private const float Penalty = 1.0f;

    public Car(float speed)
    {
        Speed = speed;
    }

    public void Reset()
    {
        X = 2;
        Speed = 1f;
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
        if (X + Width <= Lane.Width)
            X++;
    }

    public void Update(float deltaTime)
    {
        if (IsStunned)
        {
            StunRemainTick--;

            if (StunRemainTick <= 0)
            {
                IsStunned = false;
                IsInvincible = false;

                Speed -= Penalty;

                if (Speed < 1f)
                    Speed = 1f;
            }
        }

        Distance += CurrentSpeed * deltaTime;
    }

    public void OnCollision()
    {
        if (IsInvincible)
            return;

        IsStunned = true;
        IsInvincible = true;

        StunRemainTick = 40;
    }

    public void AddSpeed(float value)
    {
        Speed += value;
    }

    public void SetSpeed(float speed)
    {
        Speed = speed;
    }


}
