using System.Collections.ObjectModel;
using System.Globalization;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class Messages : ContentPage
{
    public ObservableCollection<ConversationItem> AllConversations { get; } = new();
    public ObservableCollection<ConversationItem> VisibleConversations { get; } = new();

    private string _selectedSection = "Customer";
    private string _searchText = string.Empty;
    private ConversationItem? _selectedConversation;
    private bool _loaded;

    public Messages()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyTabStyles();
        ShowEmptyThread();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadConversationsAsync();
        }
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            AllConversations.Clear();
            foreach (var conversation in await BikeMateDatabaseService.GetConversationsAsync())
            {
                AllConversations.Add(ConversationItem.FromApi(conversation));
            }

            FilterConversations(_searchText);
            ConversationCollectionView.SelectedItem = VisibleConversations.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ShowEmptyThread($"Unable to load conversations from API: {ex.Message}");
        }
    }

    private void CustomerTab_Clicked(object? sender, EventArgs e) => SelectSection("Customer");
    private void AdminTab_Clicked(object? sender, EventArgs e) => SelectSection("Admin");
    private void MechanicTab_Clicked(object? sender, EventArgs e) => SelectSection("Mechanic");
    private async void Reload_Clicked(object? sender, EventArgs e) => await LoadConversationsAsync();

    private void SelectSection(string section)
    {
        _selectedSection = section;
        ApplyTabStyles();
        FilterConversations(_searchText);
        ConversationCollectionView.SelectedItem = VisibleConversations.FirstOrDefault();
    }

    private void ApplyTabStyles()
    {
        ApplyTab(CustomerTabButton, _selectedSection == "Customer");
        ApplyTab(AdminTabButton, _selectedSection == "Admin");
        ApplyTab(MechanicTabButton, _selectedSection == "Mechanic");
    }

    private static void ApplyTab(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#FF6B2C") : Colors.White;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#242424");
        button.BorderColor = selected ? Color.FromArgb("#FF6B2C") : Color.FromArgb("#D1D5DB");
    }

    private void SearchBar_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        FilterConversations(_searchText);
        ConversationCollectionView.SelectedItem = VisibleConversations.FirstOrDefault();
    }

    private void FilterConversations(string? searchText = null)
    {
        VisibleConversations.Clear();
        string search = searchText?.Trim().ToLowerInvariant() ?? string.Empty;

        foreach (ConversationItem item in AllConversations.Where(c =>
            c.Section == _selectedSection &&
            (string.IsNullOrWhiteSpace(search) ||
             c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             c.LastMessage.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             c.DetailLine.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (c.RequestId?.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))))
        {
            VisibleConversations.Add(item);
        }
    }

    private async void ConversationCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedConversation = e.CurrentSelection.FirstOrDefault() as ConversationItem;
        if (_selectedConversation is null)
        {
            ShowEmptyThread();
            return;
        }

        ChatAvatarLabel.Text = _selectedConversation.Initials;
        ChatNameLabel.Text = _selectedConversation.Name;
        ChatStatusLabel.Text = _selectedConversation.DetailLine;
        ChatBadgeLabel.Text = _selectedConversation.BadgeText;
        ChatBadgeLabel.TextColor = _selectedConversation.BadgeTextColor;
        ChatBadgeLabel.BackgroundColor = _selectedConversation.BadgeColor;
        ReplyEntry.IsEnabled = true;
        SendButton.IsEnabled = true;
        ReplyEntry.Placeholder = $"Reply to {_selectedConversation.Name}";
        await LoadMessagesAsync(_selectedConversation.ConversationId);
    }

    private async Task LoadMessagesAsync(int conversationId)
    {
        try
        {
            ChatThreadLayout.Children.Clear();
            var messages = (await BikeMateDatabaseService.GetMessagesAsync(conversationId))
                .OrderBy(message => message.CreatedAt)
                .ToArray();
            if (messages.Length == 0)
            {
                ChatThreadLayout.Children.Add(EmptyThreadCard("No messages yet. Send a reply to begin the conversation."));
                return;
            }

            DateTime? currentDate = null;
            foreach (var message in messages)
            {
                var localDate = message.CreatedAt.ToLocalTime().Date;
                if (currentDate != localDate)
                {
                    currentDate = localDate;
                    ChatThreadLayout.Children.Add(DateDivider(localDate));
                }

                AddBubble(message);
            }
        }
        catch (Exception ex)
        {
            ChatThreadLayout.Children.Add(EmptyThreadCard($"Unable to load messages from API: {ex.Message}"));
        }
    }

    private void ShowEmptyThread(string? message = null)
    {
        ChatAvatarLabel.Text = "BM";
        ChatNameLabel.Text = "No conversation selected";
        ChatStatusLabel.Text = "Select a thread to view messages from the API.";
        ChatBadgeLabel.Text = "CHAT";
        ChatBadgeLabel.TextColor = Color.FromArgb("#F97316");
        ChatBadgeLabel.BackgroundColor = Color.FromArgb("#FFF3E8");
        ReplyEntry.Text = string.Empty;
        ReplyEntry.IsEnabled = false;
        ReplyEntry.Placeholder = "Select a thread first";
        SendButton.IsEnabled = false;
        ChatThreadLayout.Children.Clear();
        ChatThreadLayout.Children.Add(EmptyThreadCard(message ?? "Select a conversation to view messages from the API."));
    }

    private void AddBubble(AdminMessage message)
    {
        bool isMine = message.SenderUserId == AppSession.CurrentUser?.UserId;
        Color background = isMine ? Color.FromArgb("#FF8A3D") : Colors.White;
        Color textColor = isMine ? Colors.White : Color.FromArgb("#1F2937");

        var stack = new VerticalStackLayout { Spacing = 6 };
        if (!isMine && IsAutomatedMessage(message.MessageText))
        {
            stack.Add(new Label
            {
                Text = "AUTOMATED UPDATE",
                TextColor = Color.FromArgb("#F97316"),
                FontSize = 11,
                FontFamily = "PTSansCaptionBold",
                FontAttributes = FontAttributes.Bold
            });
        }

        stack.Add(new Label
        {
            Text = message.MessageText,
            TextColor = textColor,
            FontSize = 13,
            FontFamily = "PublicSans",
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (!string.IsNullOrWhiteSpace(message.AttachmentUrl))
        {
            stack.Add(new Label
            {
                Text = $"Attachment: {FileNameOrValue(message.AttachmentUrl)}",
                TextColor = isMine ? Color.FromArgb("#FFF2EA") : Color.FromArgb("#6B7280"),
                FontSize = 11,
                FontFamily = "PTSansCaption",
                LineBreakMode = LineBreakMode.TailTruncation
            });
        }

        stack.Add(new Label
        {
            Text = message.CreatedAt.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture),
            TextColor = isMine ? Color.FromArgb("#FFF2EA") : Color.FromArgb("#8A94A6"),
            FontSize = 11,
            FontFamily = "PTSansCaption",
            HorizontalTextAlignment = isMine ? TextAlignment.End : TextAlignment.Start
        });

        var bubble = new Frame
        {
            BackgroundColor = background,
            BorderColor = isMine ? background : Color.FromArgb("#E5E7EB"),
            CornerRadius = 12,
            Padding = new Thickness(12, 9),
            HasShadow = false,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            MaximumWidthRequest = 300,
            Content = stack
        };

        ChatThreadLayout.Children.Add(bubble);
    }

    private async void SendReply_Clicked(object? sender, EventArgs e)
    {
        if (_selectedConversation is null)
        {
            await DisplayAlertAsync("No Conversation", "Select a conversation before sending a reply.", "OK");
            return;
        }

        string reply = ReplyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reply))
        {
            return;
        }

        try
        {
            var sent = await BikeMateDatabaseService.SendMessageAsync(_selectedConversation.ConversationId, reply);
            AddBubble(sent);
            ReplyEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Send Failed", ex.Message, "OK");
        }
    }

    private static View EmptyThreadCard(string text)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            CornerRadius = 12,
            Padding = 14,
            HasShadow = false,
            Content = new Label
            {
                Text = text,
                Style = (Style)Application.Current!.Resources["SmallMuted"],
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View DateDivider(DateTime localDate)
    {
        return new Label
        {
            Text = localDate == DateTime.Today
                ? "Today"
                : localDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            TextColor = Color.FromArgb("#8A94A6"),
            FontSize = 11,
            FontFamily = "PTSansCaptionBold",
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4)
        };
    }

    private static bool IsAutomatedMessage(string text)
    {
        return text.Contains("booking", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("assigned", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("status", StringComparison.OrdinalIgnoreCase);
    }

    private static string FileNameOrValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "file";
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? System.IO.Path.GetFileName(uri.LocalPath)
            : System.IO.Path.GetFileName(value);
    }
}

public sealed record ConversationItem(
    int ConversationId,
    int? RequestId,
    string Section,
    string Initials,
    string Name,
    string Subtitle,
    string LastMessage,
    string Time,
    int Unread,
    string BookingStatus,
    Color AvatarColor)
{
    public bool HasUnread => Unread > 0;
    public string UnreadText => Unread > 99 ? "99+" : Unread.ToString(CultureInfo.InvariantCulture);
    public string BadgeText => Section.ToUpperInvariant();
    public Color BadgeColor => Section switch
    {
        "Customer" => Color.FromArgb("#FFF3E8"),
        "Mechanic" => Color.FromArgb("#EFF6FF"),
        _ => Color.FromArgb("#F0FDF4")
    };
    public Color BadgeTextColor => Section switch
    {
        "Customer" => Color.FromArgb("#F97316"),
        "Mechanic" => Color.FromArgb("#2563EB"),
        _ => Color.FromArgb("#16A34A")
    };
    public string DetailLine => RequestId is null
        ? Subtitle
        : $"BM-{RequestId:000000} | {FormatStatus(BookingStatus)}";
    public string PreviewText => Preview(LastMessage);

    public static ConversationItem FromApi(AdminConversation conversation)
    {
        var section = ClassifySection(conversation);
        var name = string.IsNullOrWhiteSpace(conversation.Title) ? $"Conversation {conversation.ConversationId}" : conversation.Title;
        return new ConversationItem(
            conversation.ConversationId,
            conversation.RequestId,
            section,
            BuildInitials(name),
            name,
            conversation.Subtitle ?? "BikeMate conversation",
            conversation.LastMessageText ?? conversation.Subtitle ?? "No messages yet",
            FriendlyTime(conversation.LastMessageAt),
            conversation.UnreadCount,
            conversation.BookingStatus ?? "pending",
            section switch
            {
                "Customer" => Color.FromArgb("#FF8A3D"),
                "Mechanic" => Color.FromArgb("#2563EB"),
                _ => Color.FromArgb("#16A34A")
            });
    }

    private static string ClassifySection(AdminConversation conversation)
    {
        var type = conversation.ConversationType.Trim().ToLowerInvariant();
        if (type.Contains("mechanic", StringComparison.OrdinalIgnoreCase) ||
            (conversation.Subtitle?.Contains("mechanic", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return "Mechanic";
        }

        if (conversation.RequestId is not null ||
            type.Contains("booking", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("service", StringComparison.OrdinalIgnoreCase))
        {
            return "Customer";
        }

        return "Admin";
    }

    private static string BuildInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "BM";
        if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string FriendlyTime(DateTime? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var local = value.Value.ToLocalTime();
        return local.Date == DateTime.Today
            ? local.ToString("h:mm tt", CultureInfo.InvariantCulture)
            : local.Year == DateTime.Today.Year
                ? local.ToString("MMM d", CultureInfo.InvariantCulture)
                : local.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatStatus(string status)
    {
        return string.Join(" ", status
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())));
    }

    private static string Preview(string text)
    {
        return string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}
