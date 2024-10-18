using AutoCommand.Handler;
using AutoCommand.Utils;
using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Survey.Database;
using Survey.Database.Models;

namespace Survey.Commands;

public class SuggestCommandHandler : ICommandHandler
{
    private const int MaxSize = 5 * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private readonly string _iconsPath;
    private readonly ILogger _logger;

    public SuggestCommandHandler(string iconsPath)
    {
        _iconsPath = iconsPath;
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

        if (Directory.Exists(_iconsPath))
        {
            _logger.Information("Icons directory already existing, skipping creation");
        }
        else
        {
            var directoryInfo = Directory.CreateDirectory(_iconsPath);

            if (directoryInfo.Exists)
                _logger.Information("Created icons directory successfully");
            else
                _logger.Error("Failed to create icons directory");
        }
    }

    public string CommandName => "suggest";
    public bool IsLongRunning => true;

    public async Task<string> HandleAsync(SocketSlashCommand command)
    {
        var logger = _logger.ForContext("Token", command.Token);
        var response = "Your suggestion has been saved successfully, thank you!";
        var options = command.Data.Options;
        var name = GetOption<string>(options, "name", ApplicationCommandOptionType.String);
        var note = GetOption<string>(options, "note", ApplicationCommandOptionType.String);
        var minimum = GetOption<long>(options, "minimum", ApplicationCommandOptionType.Integer);
        var maximum = GetOption<long>(options, "maximum", ApplicationCommandOptionType.Integer);
        var icon = GetOption<IAttachment>(options, "icon", ApplicationCommandOptionType.Attachment);
        var id = Guid.NewGuid();
        var path = icon is null ? null : Path.Combine(_iconsPath, id + Path.GetExtension(icon.Filename));
        var suggestion = new Suggestion(id, name ?? string.Empty, note, path, minimum, maximum);

        logger.Information("User {@User} executed command {@Command} with suggestion {@Suggestion}",
            command.User, command, suggestion);

        if (string.IsNullOrWhiteSpace(name))
            return "Please provide a name.";

        if (minimum <= 0)
            return "Please provide a valid minimum amount of players required.";

        if (maximum <= 0 || maximum < minimum || maximum >= 100)
            return "Please provide a valid maximum amount of players possible.";

        if (DatabaseHelper.GetSuggestions().Contains(suggestion))
            return "This suggestion has already been made.";

        if (!DatabaseHelper.AddSuggestion(suggestion))
            return "Something went wrong with saving your suggestion, please try again later.";

        if (icon is { Size: <= MaxSize } && icon.ContentType.StartsWith("image") && path is not null)
        {
            logger.Information("Saving image ({@ContentType}, {@Size} bytes) to {@Path}",
                icon.ContentType, icon.Size, path);

            await SaveImageAsync(icon.Url, path);
        }
        else
        {
            return "Your icon could not be saved, either because it is too big (>5MB) or not an image.";
        }

        return response;
    }

    private static T? GetOption<T>(
        IEnumerable<SocketSlashCommandDataOption> options,
        string name,
        ApplicationCommandOptionType type)
    {
        var option = Array.Find(options.ToArray(), option => option.Name == name && option.Type == type);

        return option?.Value is T value ? value : default;
    }

    private static async Task SaveImageAsync(string url, string path)
    {
        await using var imageStream = await HttpClient.GetStreamAsync(url);
        await using var fileStream = File.Create(path);
        await imageStream.CopyToAsync(fileStream);
    }
}