using AutoCommand.Handler;
using Discord;
using Discord.WebSocket;
using Serilog;
using Survey.Database;
using Survey.Database.Models;

namespace Survey.Commands;

public class SuggestCommandHandler : ICommandHandler
{
    public string CommandName => "suggest";

    public async Task HandleAsync(ILogger logger, SocketSlashCommand command)
    {
        await command.DeferAsync(true);

        var options = command.Data.Options;
        var name = GetOption<string>(options, "name", ApplicationCommandOptionType.String);
        var note = GetOption<string>(options, "note", ApplicationCommandOptionType.String);
        var icon = GetOption<IAttachment>(options, "icon", ApplicationCommandOptionType.Attachment);
        var minimum = GetOption<long>(options, "minimum", ApplicationCommandOptionType.Integer);
        var maximum = GetOption<long>(options, "maximum", ApplicationCommandOptionType.Integer);
        var userId = command.User.Id;
        var suggestion = new Suggestion(name ?? string.Empty, note, icon?.Url, minimum, maximum, userId);

        logger.Information("User {@User} executed command {@Command} with suggestion {@Suggestion}",
            command.User, command, suggestion);

        if (string.IsNullOrWhiteSpace(name))
        {
            await command.FollowupWithEmbed("Please provide a name.", Color.Red);
            return;
        }

        if (minimum <= 0)
        {
            await command.FollowupWithEmbed("Please provide a valid minimum amount of players required.", Color.Red);
            return;
        }

        if (maximum <= 0 || maximum < minimum || maximum >= 100)
        {
            await command.FollowupWithEmbed("Please provide a valid maximum amount of players possible.", Color.Red);
            return;
        }

        if (DatabaseHelper.GetSuggestions().Contains(suggestion))
        {
            await command.FollowupWithEmbed("This suggestion has already been made.", Color.Red);
            return;
        }

        if (!DatabaseHelper.AddSuggestion(suggestion))
        {
            await command.FollowupWithEmbed(
                "Something went wrong while saving your suggestion, please try again later.", Color.Red);
            return;
        }

        await command.FollowupWithEmbed("Your suggestion has been saved successfully, thank you!", Color.Green);
    }

    private static T? GetOption<T>(
        IEnumerable<SocketSlashCommandDataOption> options, string name, ApplicationCommandOptionType type)
    {
        var option = Array.Find(options.ToArray(), option => option.Name == name && option.Type == type);

        return option?.Value is T value ? value : default;
    }
}