using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Messages : ContentPage
{
    private readonly Dictionary<string, List<Conversation>> _conversationSections = new();
    private string _activeSection = "Customers";

    public Messages()
    {
        InitializeComponent();
        _conversationSections["Customers"] = new()
        {
            new Conversation("Juan dela Cruz", "Is my bike ready now?", "2"),
            new Conversation("Maria Santos", "Can I reschedule to 3PM?", "New")
        };
        _conversationSections["Admins"] = new()
        {
            new Conversation("Admin Team", "Low stock report is ready for review.", "3"),
            new Conversation("Ana Reyes", "I updated the helmet inventory.", "")
        };
        _conversationSections["Mechanics"] = new()
        {
            new Conversation("Mechanic: Isaiah", "I finished the Makati booking.", "New"),
            new Conversation("Mechanic: Marco", "Currently dispatched to Las Pinas.", "")
        };
        RefreshConversations();
    }

    private void RefreshConversations()
    {
        var conversations = _conversationSections[_activeSection];
        SectionTitleLabel.Text = $"{_activeSection} Messages";
        ConversationListLabel.Text = string.Join(
            Environment.NewLine,
            conversations.Select(conversation =>
                string.IsNullOrWhiteSpace(conversation.Badge)
                    ? $"{conversation.Title}: {conversation.LastMessage}"
                    : $"{conversation.Title}: {conversation.LastMessage} [{conversation.Badge}]"));

        var first = conversations.FirstOrDefault();
        if (first is null)
        {
            return;
        }

        OtherBubbleOneLabel.Text = first.LastMessage;
        MyBubbleLabel.Text = _activeSection == "Mechanics"
            ? "Copy. Please update status after arrival."
            : "Hi, we are checking this now.";
        OtherBubbleTwoLabel.Text = _activeSection == "Admins"
            ? "Noted, I will update the board."
            : "Thank you!";
        ApplyActiveTab();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var message = MessageEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Message Needed", "Type a message before sending.", "OK");
            return;
        }

        var conversations = _conversationSections[_activeSection];
        if (conversations.Count > 0)
        {
            conversations[0] = conversations[0] with { LastMessage = $"You: {message}", Badge = "Sent" };
        }

        MessageEntry.Text = string.Empty;
        RefreshConversations();
        await DisplayAlert("Sent", "Message saved in this conversation.", "OK");
    }

    private void OnCustomersTabClicked(object sender, EventArgs e) => SetActiveSection("Customers");
    private void OnAdminsTabClicked(object sender, EventArgs e) => SetActiveSection("Admins");
    private void OnMechanicsTabClicked(object sender, EventArgs e) => SetActiveSection("Mechanics");

    private void SetActiveSection(string section)
    {
        _activeSection = section;
        RefreshConversations();
    }

    private void ApplyActiveTab()
    {
        CustomersTabButton.BackgroundColor = _activeSection == "Customers" ? Color.FromArgb("#0D1B2A") : Colors.White;
        AdminsTabButton.BackgroundColor = _activeSection == "Admins" ? Color.FromArgb("#0D1B2A") : Colors.White;
        MechanicsTabButton.BackgroundColor = _activeSection == "Mechanics" ? Color.FromArgb("#0D1B2A") : Colors.White;

        CustomersTabButton.TextColor = _activeSection == "Customers" ? Colors.White : Color.FromArgb("#0D1B2A");
        AdminsTabButton.TextColor = _activeSection == "Admins" ? Colors.White : Color.FromArgb("#0D1B2A");
        MechanicsTabButton.TextColor = _activeSection == "Mechanics" ? Colors.White : Color.FromArgb("#0D1B2A");
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnNotificationsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Notifications());
    private async void OnProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ShopProfile());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());

    private sealed record Conversation(string Title, string LastMessage, string Badge);
}
