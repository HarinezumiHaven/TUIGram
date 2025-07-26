using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WTelegram;
using TL;
using Spectre.Console;

public class ChatInfo
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public long Id { get; set; }
    public InputPeer? InputPeer { get; set; }
}

public class ChatManager
{
    private readonly Client _client;
    private readonly List<ChatInfo> _chats = new();

    public ChatManager(Client client)
    {
        _client = client;
    }

    public async Task StartChatInterface()
    {
        // Load and display chats
        await LoadChats();
        
        while (true)
        {
            var selectedChat = await SelectChat();
            if (selectedChat == null)
                break;

            await EnterChatSession(selectedChat);
        }
    }

    private async Task LoadChats()
    {
        try
        {
            var dialogsBase = await AnsiConsole.Status()
                .StartAsync("Loading your chats...", async ctx =>
                {
                    return await _client.Messages_GetDialogs();
                });

            _chats.Clear();

            if (dialogsBase is Messages_Dialogs md)
                ProcessDialogs(md.dialogs, md.users, md.chats);
            else if (dialogsBase is Messages_DialogsSlice mds)
                ProcessDialogs(mds.dialogs, mds.users, mds.chats);

            AnsiConsole.MarkupLine($"[green]Loaded {_chats.Count} chats![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading chats: {ex.Message}[/]");
        }
    }

    private void ProcessDialogs(DialogBase[] dialogs, Dictionary<long, User> users, Dictionary<long, ChatBase> chats)
    {
        foreach (var dialogBase in dialogs)
        {
            if (dialogBase is not Dialog dialog) continue;

            var chatInfo = new ChatInfo();
            
            // Use temporary variables for out parameters
            string chatType, title;
            long id;
            InputPeer? inputPeer;
            
            if (TryGetPeerInfo(dialog.peer, users, chats, out chatType, out title, out id, out inputPeer))
            {
                chatInfo.Type = chatType;
                chatInfo.Title = title;
                chatInfo.Id = id;
                chatInfo.InputPeer = inputPeer;
                _chats.Add(chatInfo);
            }
        }
    }

    private Task<ChatInfo?> SelectChat()
    {
        if (_chats.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No chats available.[/]");
            return Task.FromResult<ChatInfo?>(null);
        }

        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Select Chat").LeftJustified().Color(Color.Aqua));
        AnsiConsole.WriteLine();

        var choices = _chats.Select(c => $"[[{c.Type}]] {c.Title}").ToList();
        choices.Add("[red]← Back to Main Menu[/]");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose a chat:")
                .PageSize(15)
                .AddChoices(choices));

        if (selection.Contains("← Back to Main Menu"))
            return Task.FromResult<ChatInfo?>(null);

        var selectedIndex = choices.IndexOf(selection);
        var result = selectedIndex >= 0 && selectedIndex < _chats.Count ? _chats[selectedIndex] : null;
        return Task.FromResult(result);
    }

    private async Task EnterChatSession(ChatInfo chatInfo)
    {
        var messages = new List<MessageInfo>();
        int currentOffset = 0;
        const int messagesPerPage = 20;

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel($"[bold aqua]{chatInfo.Title}[/]")
                .Header($"[yellow]{chatInfo.Type} Chat[/]")
                .Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            // Load messages if needed
            if (messages.Count == 0)
            {
                await LoadMessages(chatInfo, messages, currentOffset, messagesPerPage);
            }

            // Display messages
            DisplayMessages(messages);
            AnsiConsole.WriteLine();

            // Show menu
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(
                        "📝 Send Message",
                        "📜 Load More Messages",
                        "🔄 Refresh Messages",
                        "← Back to Chat List"));

            switch (action)
            {
                case "📝 Send Message":
                    await SendMessage(chatInfo);
                    messages.Clear(); // Refresh messages after sending
                    break;

                case "📜 Load More Messages":
                    currentOffset += messagesPerPage;
                    await LoadMessages(chatInfo, messages, currentOffset, messagesPerPage);
                    break;

                case "🔄 Refresh Messages":
                    messages.Clear();
                    currentOffset = 0;
                    await LoadMessages(chatInfo, messages, currentOffset, messagesPerPage);
                    break;

                case "← Back to Chat List":
                    return;
            }
        }
    }

    private async Task LoadMessages(ChatInfo chatInfo, List<MessageInfo> messages, int offset, int limit)
    {
        try
        {
            if (chatInfo.InputPeer == null)
            {
                AnsiConsole.MarkupLine("[red]Invalid chat peer.[/]");
                return;
            }

            var messagesBase = await AnsiConsole.Status()
                .StartAsync("Loading messages...", async ctx =>
                {
                    return await _client.Messages_GetHistory(chatInfo.InputPeer, offset_id: offset, limit: limit);
                });

            if (messagesBase is Messages_Messages mm)
            {
                ProcessMessages(mm.messages, mm.users, messages);
            }
            else if (messagesBase is Messages_MessagesSlice mms)
            {
                ProcessMessages(mms.messages, mms.users, messages);
            }
            else if (messagesBase is Messages_ChannelMessages mcm)
            {
                ProcessMessages(mcm.messages, mcm.users, messages);
            }

        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading messages: {ex.Message}[/]");
        }
    }

    private void ProcessMessages(MessageBase[] messagesBases, Dictionary<long, User> users, List<MessageInfo> messages)
    {
        var newMessages = new List<MessageInfo>();

        foreach (var messageBase in messagesBases.Reverse()) // Reverse to show oldest first
        {
            if (messageBase is Message msg)
            {
                var messageInfo = new MessageInfo
                {
                    Id = msg.id,
                    Text = msg.message ?? "",
                    Date = msg.date,
                    FromId = msg.from_id?.ID ?? 0
                };

                // Get sender name
                if (msg.from_id != null && users.TryGetValue(msg.from_id.ID, out var user))
                {
                    messageInfo.SenderName = $"{user.first_name} {user.last_name}".Trim();
                    if (string.IsNullOrWhiteSpace(messageInfo.SenderName))
                        messageInfo.SenderName = user.username ?? $"User {user.id}";
                }
                else
                {
                    messageInfo.SenderName = "Unknown";
                }

                newMessages.Add(messageInfo);
            }
        }

        // Add new messages to the beginning of the list
        messages.InsertRange(0, newMessages);
    }

    private void DisplayMessages(List<MessageInfo> messages)
    {
        if (messages.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No messages to display.[/]");
            return;
        }

        var table = new Table().Expand();
        table.AddColumn(new TableColumn("Time").Width(8));
        table.AddColumn(new TableColumn("Sender").Width(15));
        table.AddColumn(new TableColumn("Message"));

        // Show last 10 messages
        var recentMessages = messages.TakeLast(10);

        foreach (var msg in recentMessages)
        {
            var time = msg.Date.ToString("HH:mm");  // Direct DateTime formatting
            var sender = msg.SenderName.Length > 13 ? msg.SenderName[..13] + "..." : msg.SenderName;
            var text = string.IsNullOrEmpty(msg.Text) ? "[dim]<no text>[/]" : Markup.Escape(msg.Text);
            
            // Truncate long messages
            if (text.Length > 80)
                text = text[..77] + "...";

            table.AddRow(time, $"[aqua]{sender}[/]", text);
        }

        AnsiConsole.Write(table);
    }

    private async Task SendMessage(ChatInfo chatInfo)
    {
        try
        {
            if (chatInfo.InputPeer == null)
            {
                AnsiConsole.MarkupLine("[red]Cannot send message to this chat.[/]");
                return;
            }

            var messageText = AnsiConsole.Ask<string>("Enter your message:");

            if (string.IsNullOrWhiteSpace(messageText))
            {
                AnsiConsole.MarkupLine("[yellow]Message cannot be empty.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Sending message...", async ctx =>
                {
                    // Generate random ID for message
                    var random = new Random();
                    var randomId = random.NextInt64();
                    
                    await _client.Messages_SendMessage(chatInfo.InputPeer, messageText, randomId);
                });

            AnsiConsole.MarkupLine("[green]Message sent successfully![/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error sending message: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
        }
    }

    private static bool TryGetPeerInfo(object peer, Dictionary<long, User> users, Dictionary<long, ChatBase> chats, 
        out string chatType, out string title, out long id, out InputPeer? inputPeer)
    {
        chatType = "Unknown";
        title = "Unknown";
        id = 0;
        inputPeer = null;

        switch (peer)
        {
            case PeerUser pu:
                chatType = "Private";
                id = pu.user_id;
                
                if (users.TryGetValue(pu.user_id, out var user))
                {
                    title = $"{user.first_name} {user.last_name}".Trim();
                    if (string.IsNullOrWhiteSpace(title))
                        title = user.username ?? $"User {user.id}";
                    
                    inputPeer = new InputPeerUser(pu.user_id, user.access_hash);
                }
                return true;

            case PeerChat pc:
                chatType = "Group";
                id = pc.chat_id;
                inputPeer = new InputPeerChat(pc.chat_id);
                
                if (chats.TryGetValue(pc.chat_id, out var chat))
                    title = GetChatTitle(chat);
                return true;

            case PeerChannel pch:
                id = pch.channel_id;
                
                if (chats.TryGetValue(pch.channel_id, out var channel))
                {
                    title = GetChatTitle(channel);
                    chatType = (channel is Channel ch && ch.IsGroup) ? "Supergroup" : "Channel";
                    
                    if (channel is Channel channelObj)
                        inputPeer = new InputPeerChannel(pch.channel_id, channelObj.access_hash);
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

    private static string GetChatTitle(ChatBase chat) => chat switch
    {
        Chat c => c.title,
        Channel ch => ch.title,
        ChatForbidden cf => cf.title,
        ChannelForbidden chf => chf.title,
        _ => "Unknown Chat"
    };
}

public class MessageInfo
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime Date { get; set; }  // Changed from int to DateTime
    public long FromId { get; set; }
    public string SenderName { get; set; } = "";
}