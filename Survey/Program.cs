using AutoCommand.Handler;
using AutoCommand.Utils;
using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace Survey;

internal static class Program
{
    private static readonly string CommandPath = EnvironmentUtils.GetVariable("SURVEY_COMMAND_PATH", "config");
    private static readonly string LogPath = EnvironmentUtils.GetVariable("SURVEY_LOG_FILE", "survey-.log");
    private static readonly string Token = EnvironmentUtils.GetVariable("SURVEY_DISCORD_TOKEN");

    private static readonly DiscordSocketClient Client = new(new DiscordSocketConfig
    {
        DefaultRetryMode = RetryMode.RetryRatelimit,
        LogLevel = LogSeverity.Info,
        GatewayIntents = GatewayIntents.Guilds
    });

    private static readonly Logger Logger = new LoggerConfiguration()
        .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
        .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day)
        .CreateLogger();

    private static readonly DefaultCommandHandler CommandHandler = new(logPath: LogPath);

    private static async Task Main()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            Logger.Fatal("Environment variable `SURVEY_DISCORD_TOKEN` is missing");
            return;
        }

        InitializeDiscordClient();

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static void InitializeDiscordClient()
    {
        Client.Log += message =>
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Logger.Fatal(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    Logger.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    Logger.Warning(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    Logger.Information(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    Logger.Verbose(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    Logger.Debug(message.Exception, message.Message);
                    break;
                default:
                    Logger.Information(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        };
        Client.Ready += () => Client.CreateSlashCommands(CommandPath);
        Client.Ready += () =>
        {
            Client.SlashCommandExecuted += command => CommandHandler.HandleAsync(command);
            return Task.CompletedTask;
        };
        Client.Ready += async () =>
        {
            await Client.SetCustomStatusAsync("Ready for takeoff!");
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
        };
    }
}