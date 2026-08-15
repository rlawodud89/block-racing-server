
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
                ? Speed * 0.2f
                : Speed;
        }
    }

    public float Distance { get; private set; }

    public bool IsStunned { get; private set; }

    public bool IsInvincible { get; private set; }

    public int StunRemainTick { get; private set; }

    private const float Penalty = 1.0f;
    private const float LineClearSpeedBonus = 0.05f;
    private const float MaxSpeed = 3.0f;

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

        Speed -= Penalty;

        if (Speed < 1f)
            Speed = 1f;
    }

    public void OnCollision()
    {
        if (IsInvincible)
            return;

        IsStunned = true;
        IsInvincible = true;

        StunRemainTick = 40;
    }

    public void AddLineClearSpeed(int lineCount)
    {
        Speed = MathF.Min(
            Speed + lineCount * LineClearSpeedBonus,
            MaxSpeed
        );
    }
}
