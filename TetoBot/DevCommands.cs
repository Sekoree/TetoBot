using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace TetoBot;

public class DevCommands : ApplicationCommandsModule
{
    [SlashCommand("changeplaying", "Change the bot status"), ApplicationCommandRequireUserPermissions(Permissions.ManageGuild)]
    public async Task ChangePlaying(InteractionContext ctx, [Option("status", "The status to change to")] string status, [Option("type", "The type of status to change to")] ActivityType type = ActivityType.Playing)
    {
        var presence = new DiscordActivity(status, type);
        await ctx.Client.UpdateStatusAsync(presence);
    }
}