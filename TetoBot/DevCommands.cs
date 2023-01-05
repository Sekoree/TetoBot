using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;

namespace TetoBot;

public class DevCommands : ApplicationCommandsModule
{

    [SlashCommand("changeplaying", "Change the bot status")]
    public async Task ChangePlaying()
    {
        
    }
}