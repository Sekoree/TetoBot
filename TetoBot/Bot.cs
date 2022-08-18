using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Logging;

namespace TetoBot;

public class Bot : IDisposable, IAsyncDisposable
{
    private DiscordClient client { get; set; }
    private bool initialized { get; set; } = false;

    public Bot(string token)
    {
        var clientConfig = new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.GuildVoiceStates | DiscordIntents.Guilds,
            MinimumLogLevel = LogLevel.Information
        };
        client = new DiscordClient(clientConfig);
        client.Ready += OnReady;
        client.GuildAvailable += OnGuildAvailable;
        client.VoiceStateUpdated += OnVoiceStateUpdated;
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        sender.Logger.LogInformation("Guild available: {GuildName}", e.Guild.Name);
        _ = Task.Run(() => InitOnGuild(e.Guild));
        //foreach (var role in e.Guild.Roles)
        //{
        //    //log role ID and name
        //    sender.Logger.LogInformation("Role: {RoleName} {RoleID}", role.Value.Name, role.Key);
        //}
        return Task.CompletedTask;
    }

    private Task OnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        if (!initialized)
        {
            sender.Logger.LogInformation("Voice state updated: {UserName} while not initialized", e.User.Username);
            return Task.CompletedTask;
        }
        //check if user left voice channel
        if (e.Before?.Channel != null && e.After?.Channel == null)
        {
            sender.Logger.LogDebug("User left voice channel: {UserName}", e.User.Username);
            _ = Task.Run(() => HandleRemoveRole(e));
        }
        //check if user joined voice channel
        else if (e.Before?.Channel == null && e.After?.Channel != null)
        {
            sender.Logger.LogDebug("User joined voice channel: {UserName}", e.User.Username);
            _ = Task.Run(() => HandleAddRole(e));
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleAddRole(VoiceStateUpdateEventArgs e)
    {
        try
        {
            var role = e.Guild.GetRole(1007978361223643196);
            var userAsMember = await e.Guild.GetMemberAsync(e.User.Id);
            await userAsMember.GrantRoleAsync(role);
            client.Logger.LogInformation("Added voice role to {UserName}", e.User.Username);
        }
        catch (Exception exception)
        {
            client.Logger.LogError(exception, "Error adding role");
        }
    }

    private async Task HandleRemoveRole(VoiceStateUpdateEventArgs e)
    {
        try
        {
            var role = e.Guild.GetRole(1007978361223643196);
            var userAsMember = await e.Guild.GetMemberAsync(e.User.Id);
            await userAsMember.RevokeRoleAsync(role);
            client.Logger.LogInformation("Removed voice role from {UserName}", e.User.Username);
        }
        catch (Exception exception)
        {
            client.Logger.LogError(exception, "Error while removing role");
        }
    }

    private async Task InitOnGuild(DiscordGuild g)
    {
        client.Logger.LogInformation("Initializing on guild: {0}", g.Name);
        try
        {
            var role = g.GetRole(1007978361223643196);
            //client.Logger.LogInformation("Role found: {0}", role.Name);
            var members = await g.GetAllMembersAsync();
            foreach (var member in members)
            {
                if (member.VoiceState?.Channel != null 
                    && member.Roles.All(x => x.Id != role.Id))
                {
                    try
                    {
                        await member.GrantRoleAsync(role);
                        client.Logger.LogInformation("{MemberUsername} has been added to the {RoleName} role", member.Username, role.Name);
                    }
                    catch (Exception e)
                    {
                        client.Logger.LogError(e, "Error adding role to {MemberUsername}", member?.Username);
                    }
                }
                else if (member.VoiceState?.Channel == null 
                         && member.Roles.Any(x => x.Id == role.Id))
                {
                    try
                    {
                        await member.RevokeRoleAsync(role);
                        client.Logger.LogInformation("{0} has been removed from the {1} role", member.Username, role.Name);
                    }
                    catch (Exception e)
                    {
                        client.Logger.LogError(e, "Error removing role from {MemberUsername}", member?.Username);
                    }
                }
            }
        }
        catch (Exception e)
        {
            client.Logger.LogError(e, "Error initializing on guild: {GuildName}", g.Name);
        }
        initialized = true;
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        client.Logger.Log(LogLevel.Information, "Connected to Discord!");
        _ = Task.Run(SetBotStatusAsync);
        return Task.CompletedTask;
    }
    
    public async Task SetBotStatusAsync()
    {
        while (true)
        {
            var activity = new DiscordActivity("TETO TETO TETO TETO TETO...", ActivityType.Playing);
            await client.UpdateStatusAsync(activity);
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

    
    public async Task RunAsync()
    {
        await client.ConnectAsync();
    }


    public void Dispose() 
        => DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        await client.DisconnectAsync();
        client.Dispose();
        //throw new NotImplementedException();
    }
}