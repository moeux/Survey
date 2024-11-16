using AutoCommand.Handler;
using Discord;
using Discord.WebSocket;
using Serilog;
using Survey.Database;

namespace Survey.Commands;

public class PollCommandHandler : ICommandHandler
{
    private const string SelectMenuSmallId = "poll-creator-select-menu-small";
    private const string SelectMenuMediumId = "poll-creator-select-menu-mid";
    private const string SelectMenuLargeId = "poll-creator-select-menu-large";
    private const string CreateButtonId = "poll-creator-button";
    public string CommandName => "poll";

    public async Task HandleAsync(ILogger logger, SocketSlashCommand command)
    {
        logger.Information("User {@User} executed command {@Command}", command.User, command);

        await command.DeferAsync(true);

        if (command.User is not SocketGuildUser user)
        {
            await command.FollowupWithEmbedAsync("Something went wrong. Please try again later.", Color.Red);
            return;
        }

        if (!user.GuildPermissions.Has(GuildPermission.Administrator))
        {
            await command.FollowupWithEmbedAsync("You don't have permission to use this command.", Color.Red);
            return;
        }

        var button = ButtonBuilder.CreateSuccessButton("Create", CreateButtonId, new Emoji("\ud83d\udcdd"));
        var messageComponent = new ComponentBuilder()
            .WithSelectMenu(CreateSelectMenu(SelectMenuSmallId, 1, 4))
            .WithSelectMenu(CreateSelectMenu(SelectMenuMediumId, 5, 8), 1)
            .WithSelectMenu(CreateSelectMenu(SelectMenuLargeId, 9, int.MaxValue), 2)
            .WithButton(button, 3)
            .Build();
        var embed = new EmbedBuilder()
            .WithTitle("Poll Creator")
            .WithDescription("Please choose the games for the poll.")
            .WithColor(Color.Green)
            .AddField("Small Games", "Games that support 4 people maximum")
            .AddField("Mid-Sized Games", "Games that support 4 people minimum and up to 8")
            .AddField("Large Games", "Games that support 9 people minimum")
            .Build();

        await command.FollowupAsync(ephemeral: true, components: messageComponent, embed: embed);
    }

    private static SelectMenuBuilder CreateSelectMenu(string customId, int minimum, int maximum)
    {
        var options = DatabaseHelper.GetSuggestions()
            .Where(suggestion => suggestion.Minimum >= minimum && suggestion.Maximum <= maximum)
            .Select(suggestion =>
                new SelectMenuOptionBuilder()
                    .WithLabel(suggestion.Name)
                    .WithDescription(suggestion.Note)
                    .WithValue(suggestion.Id.ToString()))
            .ToList();

        return new SelectMenuBuilder()
            .WithCustomId(customId)
            .WithPlaceholder("Choose at least two games")
            .WithMinValues(2)
            .WithOptions(options);
    }
}