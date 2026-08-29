
namespace block_racing_server.Data;

public static class GameBalance
{
    public static float InitialCarSpeed { get; set; }
    public static float CarSpeedPenalty { get; set; }
    public static float LineClearSpeedBonus { get; set; }
    public static float CarMaxSpeed { get; set; }
    public static int CarStunDurationTick { get; set; }
    public static float StunnedSpeedMultiplier { get; set; }

    public static float BaseScrollSpeed { get; set; }

    public static float PieceCooldownTime { get; set; }
    public static int TargetDistance { get; set; }
}