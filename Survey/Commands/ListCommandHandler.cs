using System.Collections.Immutable;
using System.Text;
using AutoCommand.Handler;
using AutoCommand.Utils;
using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Survey.Database;
using Survey.Database.Models;

namespace Survey.Commands;

public class ListCommandHandler : ICommandHandler
{
    private const int DefaultChunkSize = 10;
    private const int MaxMessageLength = 2000;
    private readonly ILogger _logger;

    public ListCommandHandler()
    {
        _logger = new LoggerConfiguration()
            .Destructure.ByTransformingWhere<dynamic>(type => typeof(SocketUser).IsAssignableFrom(type),
                user => new { user.Id, user.Username })
            .Destructure.ByTransforming<SocketSlashCommand>(command => new { command.Id, command.CommandName })
            .Enrich.FromLogContext()
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Literate,
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties:j}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                EnvironmentUtils.GetVariable("ROCKET_LOG_FILE", "rocket-.log"),
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties:j}{NewLine}{Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day)
            .CreateLogger()
            .ForContext<SuggestCommandHandler>();
    }

    public string CommandName => "list";
    public bool IsLongRunning => false;

    public Task<string> HandleAsync(SocketSlashCommand command)
    {
        var logger = _logger.ForContext("Token", command.Token);
        var pages = DatabaseHelper.GetSuggestions().Chunk(DefaultChunkSize).ToImmutableArray();
        var option = command.Data.Options.FirstOrDefault();

        logger.Information("User {@User} executed command {@Command}", command.User, command);

        if (pages.Length == 0) return Task.FromResult("There are no suggestions yet.");

        var index = option is not { Type: ApplicationCommandOptionType.Integer, Value: int input } ? 0 : input - 1;
        index = Math.Min(pages.Length - 1, Math.Max(0, index));
        var response = string.Join(
            Environment.NewLine,
            pages[index].Select((suggestion, i) => FormatSuggestion(i + 1, suggestion))
        );

        return Task.FromResult(Truncate(response, MaxMessageLength));
    }

    private static string FormatSuggestion(int index, Suggestion suggestion)
    {
        var builder = new StringBuilder($"{index}. {suggestion.Name} ");

        builder.Append($"({suggestion.Minimum}");
        if (suggestion.Minimum != suggestion.Maximum) builder.Append($"-{suggestion.Maximum}");
        builder.Append(" Players)");

        if (!string.IsNullOrWhiteSpace(suggestion.Note))
            builder.AppendLine()
                .Append('\t')
                .Append($"{suggestion.Note}");

        return builder.ToString();
    }

    private static string Truncate(string str, int length, int padding = 2)
    {
        if (string.IsNullOrWhiteSpace(str) || str.Length <= length) return str;

        return str[..(length - padding)].PadRight(padding, '.');
    }
}