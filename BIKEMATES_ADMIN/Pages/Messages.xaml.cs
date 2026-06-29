using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class Messages : ContentPage
{
    public ObservableCollection<ConversationItem> AllConversations { get; } = new();
    public ObservableCollection<ConversationItem> VisibleConversations { get; } = new();

    private string _selectedSection = "Customer";
    private ConversationItem? _selectedConversation;
    private bool _loaded;

    public Messages()
    {
        InitializeComponent();
        BindingContext = this;
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

            FilterConversations();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Messages", $"Unable to load conversations from API: {ex.Message}", "OK");
        }
    }

    private void CustomerTab_Clicked(object sender, EventArgs e) => SelectSection("Customer");
    private void AdminTab_Clicked(object sender, EventArgs e) => SelectSection("Admin");
    private void MechanicTab_Clicked(object sender, EventArgs e) => SelectSection("Mechanic");

    private void SelectSection(string section)
    {
        _selectedSection = section;
        CustomerTabButton.Style = section == "Customer" ? (Style)Application.Current!.Resources["PrimaryButton"] : (Style)Application.Current!.Resources["OutlineButton"];
        AdminTabButton.Style = section == "Admin" ? (Style)Application.Current!.Resources["PrimaryButton"] : (Style)Application.Current!.Resources["OutlineButton"];
        MechanicTabButton.Style = section == "Mechanic" ? (Style)Application.Current!.Resources["PrimaryButton"] : (Style)Application.Current!.Resources["OutlineButton"];
        FilterConversations();
        ConversationCollectionView.SelectedItem = VisibleConversations.FirstOrDefault();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e) => FilterConversations(e.NewTextValue);

    private void FilterConversations(string? searchText = null)
    {
        VisibleConversations.Clear();
        string search = searchText?.Trim().ToLowerInvariant() ?? string.Empty;

        foreach (ConversationItem item in AllConversations.Where(c =>
            c.Section == _selectedSection &&
            (string.IsNullOrWhiteSpace(search) || c.Name.ToLowerInvariant().Contains(search) || c.LastMessage.ToLowerInvariant().Contains(search))))
        {
            VisibleConversations.Add(item);
        }
    }

    private async void ConversationCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedConversation = e.CurrentSelection.FirstOrDefault() as ConversationItem;
        if (_selectedConversation is null)
        {
            ShowEmptyThread();
            return;
        }

        ChatAvatarLabel.Text = _selectedConversation.Initials;
        ChatNameLabel.Text = _selectedConversation.Name;
        await LoadMessagesAsync(_selectedConversation.ConversationId);
    }

    private async Task LoadMessagesAsync(int conversationId)
    {
        try
        {
            ChatThreadLayout.Children.Clear();
            var messages = await BikeMateDatabaseService.GetMessagesAsync(conversationId);
            if (!messages.Any())
            {
                ChatThreadLayout.Children.Add(new Label
                {
                    Text = "No messages in this conversation yet.",
                    Style = (Style)Application.Current!.Resources["SmallMuted"]
                });
                return;
            }

            foreach (var message in messages)
            {
                AddBubble(message.MessageText, message.SenderUserId == AppSession.CurrentUser?.UserId);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Messages", $"Unable to load messages from API: {ex.Message}", "OK");
        }
    }

    private void ShowEmptyThread()
    {
        ChatAvatarLabel.Text = "BM";
        ChatNameLabel.Text = "No conversation selected";
        ChatThreadLayout.Children.Clear();
        ChatThreadLayout.Children.Add(new Label
        {
            Text = "Select a conversation to view messages from the API.",
            Style = (Style)Application.Current!.Resources["SmallMuted"]
        });
    }

    private void AddBubble(string text, bool isMine)
    {
        Color background = isMine ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
        Color textColor = isMine ? Colors.White : Color.FromArgb("#1F2937");

        Frame bubble = new()
        {
            BackgroundColor = background,
            CornerRadius = 18,
            Padding = new Thickness(12, 8),
            HasShadow = false,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            MaximumWidthRequest = 250,
            Content = new Label
            {
                Text = text,
                TextColor = textColor,
                FontSize = 13,
                FontFamily = "OpenSansRegular"
            }
        };

        ChatThreadLayout.Children.Add(bubble);
    }

    private async void SendReply_Clicked(object sender, EventArgs e)
    {
        if (_selectedConversation is null)
        {
            await DisplayAlert("No Conversation", "Select a conversation before sending a reply.", "OK");
            return;
        }

        string reply = ReplyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reply))
            return;

        try
        {
            var sent = await BikeMateDatabaseService.SendMessageAsync(_selectedConversation.ConversationId, reply);
            AddBubble(sent.MessageText, true);
            ReplyEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Send Failed", ex.Message, "OK");
        }
    }
}

public sealed record ConversationItem(int ConversationId, string Section, string Initials, string Name, string LastMessage, string Time, int Unread, Color AvatarColor)
{
    public bool HasUnread => Unread > 0;

    public static ConversationItem FromApi(AdminConversation conversation)
    {
        var section = ClassifySection(conversation);
        var name = string.IsNullOrWhiteSpace(conversation.Title) ? $"Conversation {conversation.ConversationId}" : conversation.Title;
        return new ConversationItem(
            conversation.ConversationId,
            section,
            BuildInitials(name),
            name,
            conversation.LastMessageText ?? conversation.Subtitle ?? "No messages yet",
            conversation.LastMessageAt?.ToLocalTime().ToString("g") ?? string.Empty,
            conversation.UnreadCount,
            section switch
            {
                "Customer" => Color.FromArgb("#2563EB"),
                "Mechanic" => Color.FromArgb("#FF7A2D"),
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

        if (conversation.RequestId is not null || type.Contains("booking", StringComparison.OrdinalIgnoreCase) || type.Contains("service", StringComparison.OrdinalIgnoreCase))
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
}




