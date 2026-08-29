using block_racing_server.Data;
using System.Text.Json;

namespace block_racing_server.Game.Data;

public static class GameBalanceLoader
{

    public static void Load()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Config",
            "game_balance.json"
        );

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Game balance file not found: {path}"
            );

        string json = File.ReadAllText(path);

        GameBalanceConfig? config =
            JsonSerializer.Deserialize<GameBalanceConfig>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (config == null)
            throw new InvalidOperationException(
                "Failed to load game balance."
            );

        GameBalance.InitialCarSpeed = config.InitialCarSpeed;
        GameBalance.CarSpeedPenalty = config.CarSpeedPenalty;
        GameBalance.LineClearSpeedBonus = config.LineClearSpeedBonus;
        GameBalance.CarMaxSpeed = config.CarMaxSpeed;
        GameBalance.CarStunDurationTick = config.CarStunDurationTick;
        GameBalance.StunnedSpeedMultiplier = config.StunnedSpeedMultiplier;

        GameBalance.BaseScrollSpeed = config.BaseScrollSpeed;

        GameBalance.PieceCooldownTime = config.PieceCooldownTime;
        GameBalance.TargetDistance = config.TargetDistance;
    }

    private class GameBalanceConfig
    {
        public float InitialCarSpeed { get; set; }
        public float CarSpeedPenalty { get; set; }
        public float LineClearSpeedBonus { get; set; }
        public float CarMaxSpeed { get; set; }
        public int CarStunDurationTick { get; set; }
        public float StunnedSpeedMultiplier { get; set; }

        public float BaseScrollSpeed { get; set; }

        public float PieceCooldownTime { get; set; }
        public int TargetDistance { get; set; }
    }
}