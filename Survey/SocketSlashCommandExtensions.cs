using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Survey;

public static class SocketSlashCommandExtensions
{
    public static Task<RestFollowupMessage> FollowupWithEmbedAsync(
        this SocketSlashCommand command, string description, Color color)
    {
        var embed = new EmbedBuilder()
            .WithDescription(description)
            .WithColor(color)
            .Build();
        return command.FollowupAsync(ephemeral: true, embed: embed);
    }

    public static Task RespondWithEmbedAsync(
        this SocketSlashCommand command, string description, Color color)
    {
        var embed = new EmbedBuilder()
            .WithDescription(description)
            .WithColor(color)
            .Build();
        return command.RespondAsync(ephemeral: true, embed: embed);
    }
}