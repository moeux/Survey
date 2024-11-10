using System.Collections.Immutable;
using AutoCommand.Handler;
using Discord;
using Discord.WebSocket;
using Serilog;
using Survey.Database;

namespace Survey.Commands;

public class ListCommandHandler(DiscordSocketClient client) : ICommandHandler
{
    private const int DefaultChunkSize = 10;

    public string CommandName => "list";

    public async Task HandleAsync(ILogger logger, SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        var suggestions = DatabaseHelper.GetSuggestions().ToList();
        suggestions.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCulture));
        var pages = suggestions.Chunk(DefaultChunkSize).ToImmutableArray();
        var option = command.Data.Options.FirstOrDefault();

        logger.Information("User {@User} executed command {@Command}", command.User, command);

        if (pages.Length == 0)
        {
            await command.FollowupWithEmbed("There are no suggestions yet.", Color.Red);
            return;
        }

        var index = option is { Type: ApplicationCommandOptionType.Integer, Value: long input } ? (int)input - 1 : 0;
        index = Math.Min(pages.Length - 1, Math.Max(0, index));

        var embeds = await Task.WhenAll(
            pages[index].Select(async (suggestion, i) =>
            {
                var user = await client.GetUserAsync(suggestion.UserId);
                var embed = new EmbedBuilder()
                    .WithTitle(suggestion.Name)
                    .WithAuthor(user)
                    .WithColor(Color.Blue)
                    .WithFooter($"Suggestion #{i + DefaultChunkSize * index + 1}")
                    .WithTimestamp(suggestion.CreatedAt)
                    .AddField("Minimum Players", suggestion.Minimum, true)
                    .AddField("Maximum Players", suggestion.Maximum, true);

                if (!string.IsNullOrWhiteSpace(suggestion.Note)) embed.WithDescription(suggestion.Note);

                if (!string.IsNullOrWhiteSpace(suggestion.IconUrl)) embed.WithThumbnailUrl(suggestion.IconUrl);

                return embed.Build();
            })
        );

        await command.FollowupAsync(embeds: embeds, ephemeral: true);
    }
}