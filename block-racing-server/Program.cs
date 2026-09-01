using block_racing_server.Game.Data;
using block_racing_server.Network;
using Microsoft.Extensions.Logging;
using Serilog;

namespace block_racing_server;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                    "[{SourceContext}] " +
                    "{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/server-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] " +
                    "[{SourceContext}] " +
                    "{Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog(Log.Logger);
                });

            ILogger<Program> logger =
                loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Block Racing Server starting.");

            GameBalanceLoader.Load();

            TcpServer server = new(loggerFactory);

            logger.LogInformation("Starting TCP server. Port={Port}", 7777);

            await server.StartAsync(7777);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Server startup failed.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}