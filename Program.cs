using System;
using System.Threading.Tasks;
using WTelegram;
using TL;
using Spectre.Console;

public class TelegramAuth
{
    private Client? _client;
    private readonly Dictionary<string, string> _configCache = new();

    public async Task<bool> AuthorizeAsync()
    {
        try
        {
            // Pre-collect all configuration before creating client
            await CollectConfigurationAsync();
            
            AnsiConsole.MarkupLine("[yellow]Connecting to Telegram...[/]");
            
            // Create client with logging disabled
            _client = new Client(Config);
            
            // Disable logging by setting the log level to None or redirecting logs
            WTelegram.Helpers.Log = (level, message) => { }; // Suppress all logs
            
            var user = await _client.LoginUserIfNeeded();
            AnsiConsole.MarkupLine($"[green]Successfully authorized! Welcome, {user.first_name}![/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

    // Pre-collect configuration to avoid interactive prompts during Status
    private Task CollectConfigurationAsync()
    {
        _configCache["api_id"] = Environment.GetEnvironmentVariable("TELEGRAM_API_ID") 
            ?? AnsiConsole.Ask<string>("Enter your Telegram API ID:");
        
        _configCache["api_hash"] = Environment.GetEnvironmentVariable("TELEGRAM_API_HASH") 
            ?? AnsiConsole.Ask<string>("Enter your Telegram API Hash:");
        
        _configCache["phone_number"] = AnsiConsole.Ask<string>("Enter your phone number (with country code, e.g., +1234567890):");
        
        _configCache["session_pathname"] = "session.dat";
        
        return Task.CompletedTask;
    }

    // Config method that uses cached values or prompts when needed
    private string? Config(string what)
    {
        // Return cached values if available
        if (_configCache.TryGetValue(what, out string? cachedValue))
            return cachedValue;

        // Handle runtime prompts (verification code, password, etc.)
        return what switch
        {
            "verification_code" => AnsiConsole.Ask<string>("Enter verification code sent to your Telegram:"),
            "password" => AnsiConsole.Prompt(new TextPrompt<string>("Enter your 2FA password:").Secret()),
            "first_name" => AnsiConsole.Ask<string>("Enter your first name:"),
            "last_name" => AnsiConsole.Ask<string>("Enter your last name (or press Enter to skip):"),
            _ => null
        };
    }

    public Client? GetClient() => _client;

    public void Dispose() => _client?.Dispose();
}

class Program
{
    static async Task Main(string[] args)
    {
        // Suppress WTelegram logging at the very beginning
        WTelegram.Helpers.Log = (level, message) => { };
        
        AnsiConsole.Write(new FigletText("Telegram TUI").LeftJustified().Color(Color.Blue));
        AnsiConsole.MarkupLine("[dim]A Terminal User Interface for Telegram[/]");
        AnsiConsole.WriteLine();

        var auth = new TelegramAuth();

        try
        {
            // Don't use Status wrapper around authorization since it needs interactive input
            bool isAuthorized = await auth.AuthorizeAsync();

            if (!isAuthorized)
            {
                AnsiConsole.MarkupLine("[red]Failed to authorize. Exiting...[/]");
                return;
            }

            var client = auth.GetClient();
            if (client == null)
            {
                AnsiConsole.MarkupLine("[red]Client is null. Exiting...[/]");
                return;
            }

            var me = client.User;
            AnsiConsole.MarkupLine($"[green]Welcome, {me.first_name}![/]");
            AnsiConsole.WriteLine();

            await ShowMainMenu(client);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            auth.Dispose();
        }
    }

    // Main menu with options
    private static async Task ShowMainMenu(Client client)
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Main Menu").LeftJustified().Color(Color.Green));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(
                        "💬 Open Chats (Interactive)",
                        "📋 Show All Chats (Table View)",
                        "❌ Exit"));

            switch (choice)
            {
                case "💬 Open Chats (Interactive)":
                    var chatManager = new ChatManager(client);
                    await chatManager.StartChatInterface();
                    break;

                case "📋 Show All Chats (Table View)":
                    await ShowDialogsTable(client);
                    break;

                case "❌ Exit":
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    return;
            }
        }
    }

    // Original dialog table view (kept for reference)
    private static async Task ShowDialogsTable(Client client)
    {
        try
        {
            var dialogsBase = await AnsiConsole.Status()
                .StartAsync("Loading your chats...", async ctx =>
                {
                    return await client.Messages_GetDialogs();
                });

            AnsiConsole.MarkupLine("[green]Chats loaded![/]");

            var table = new Table().Centered();
            table.AddColumn("Type");
            table.AddColumn("Title/Name");
            table.AddColumn("ID");

            // Handle both dialog types from WTelegramClient
            if (dialogsBase is Messages_Dialogs md)
                AddDialogsToTable(md.dialogs, md.users, md.chats, table);
            else if (dialogsBase is Messages_DialogsSlice mds)
                AddDialogsToTable(mds.dialogs, mds.users, mds.chats, table);
            else
                AnsiConsole.MarkupLine("[yellow]Unexpected dialog type received.[/]");

            if (table.Rows.Count > 0)
                AnsiConsole.Write(table);
            else
                AnsiConsole.MarkupLine("[yellow]No dialogs found.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading chats: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to return to main menu...[/]");
        Console.ReadKey();
    }

    // Add dialog info to the table
    private static void AddDialogsToTable(DialogBase[] dialogs, Dictionary<long, User> users, Dictionary<long, ChatBase> chats, Table table)
    {
        foreach (var dialogBase in dialogs)
        {
            if (dialogBase is not Dialog dialog) continue;

            string chatType = "Unknown";
            string title = "Unknown";
            string id = "Unknown";

            if (!TryGetPeerInfo(dialog.peer, users, chats, out chatType, out title, out id))
                title = "Unknown Peer";

            table.AddRow(chatType, title, id);
        }
    }

    // Extract peer info (type, title, id)
    private static bool TryGetPeerInfo(object peer, Dictionary<long, User> users, Dictionary<long, ChatBase> chats, out string chatType, out string title, out string id)
    {
        chatType = "Unknown";
        title = "Unknown";
        id = "Unknown";

        switch (peer)
        {
            case PeerUser pu:
                chatType = "Private";
                id = pu.user_id.ToString();
                if (users.TryGetValue(pu.user_id, out var user))
                {
                    title = $"{user.first_name} {user.last_name}".Trim();
                    if (string.IsNullOrWhiteSpace(title))
                        title = user.username ?? $"User {user.id}";
                }
                return true;

            case PeerChat pc:
                chatType = "Group";
                id = pc.chat_id.ToString();
                if (chats.TryGetValue(pc.chat_id, out var chat))
                    title = GetChatTitle(chat);
                return true;

            case PeerChannel pch:
                id = pch.channel_id.ToString();
                if (chats.TryGetValue(pch.channel_id, out var channel))
                {
                    title = GetChatTitle(channel);
                    chatType = (channel is Channel ch && ch.IsGroup) ? "Supergroup" : "Channel";
                }
                else
                {
                    chatType = "Channel";
                }
                return true;

            default:
                return false;
        }
    }

    // Get chat/channel title
    private static string GetChatTitle(ChatBase chat) => chat switch
    {
        Chat c => c.title,
        Channel ch => ch.title,
        ChatForbidden cf => cf.title,
        ChannelForbidden chf => chf.title,
        _ => "Unknown Chat"
    };
}