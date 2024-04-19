using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Logging;

namespace TetoBot;

public class Bot : IDisposable, IAsyncDisposable
{
    private DiscordClient _client;
    private ApplicationCommandsExtension _commands;
    private bool _initialized = false;
    public static string Presence = "TETO TETO TETO TETO TETO...";

    public Bot(string token)
    {
        var clientConfig = new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.GuildVoiceStates | DiscordIntents.Guilds,
            MinimumLogLevel = LogLevel.Information
        };
        _client = new DiscordClient(clientConfig);
        _client.Ready += OnReady;
        _client.GuildAvailable += OnGuildAvailable;
        _client.VoiceStateUpdated += OnVoiceStateUpdated;
        _commands =_client.UseApplicationCommands();
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
        if (!_initialized)
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
            _client.Logger.LogInformation("Added voice role to {UserName}", e.User.Username);
        }
        catch (Exception exception)
        {
            _client.Logger.LogError(exception, "Error adding role");
        }
    }

    private async Task HandleRemoveRole(VoiceStateUpdateEventArgs e)
    {
        try
        {
            var role = e.Guild.GetRole(1007978361223643196);
            var userAsMember = await e.Guild.GetMemberAsync(e.User.Id);
            await userAsMember.RevokeRoleAsync(role);
            _client.Logger.LogInformation("Removed voice role from {UserName}", e.User.Username);
        }
        catch (Exception exception)
        {
            _client.Logger.LogError(exception, "Error while removing role");
        }
    }

    private async Task InitOnGuild(DiscordGuild g)
    {
        _client.Logger.LogInformation("Initializing on guild: {0}", g.Name);
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
                        _client.Logger.LogInformation("{MemberUsername} has been added to the {RoleName} role",
                            member.Username, role.Name);
                    }
                    catch (Exception e)
                    {
                        _client.Logger.LogError(e, "Error adding role to {MemberUsername}", member?.Username);
                    }
                }
                else if (member.VoiceState?.Channel == null
                         && member.Roles.Any(x => x.Id == role.Id))
                {
                    try
                    {
                        await member.RevokeRoleAsync(role);
                        _client.Logger.LogInformation("{0} has been removed from the {1} role", member.Username,
                            role.Name);
                    }
                    catch (Exception e)
                    {
                        _client.Logger.LogError(e, "Error removing role from {MemberUsername}", member?.Username);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _client.Logger.LogError(e, "Error initializing on guild: {GuildName}", g.Name);
        }

        _initialized = true;
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        _client.Logger.Log(LogLevel.Information, "Connected to Discord!");
        _commands.RegisterGuildCommands<DevCommands>(588821508990959634);
        return Task.CompletedTask;
    }

    public async Task RunAsync()
    {
        await _client.ConnectAsync(new DiscordActivity(Presence, ActivityType.Playing));
    }


    public void Dispose()
        => DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        await _client.DisconnectAsync();
        _client.Dispose();
        //throw new NotImplementedException();
    }
}