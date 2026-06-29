using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Operations : ContentPage
{
    private readonly List<OperationTask> _tasks = new();

    public Operations()
    {
        InitializeComponent();
        StatusFilterPicker.ItemsSource = new[] { "All", "Pending", "Active", "Completed" };
        PriorityPicker.ItemsSource = new[] { "Normal", "High", "Urgent" };
        StatusFilterPicker.SelectedIndex = 0;
        PriorityPicker.SelectedIndex = 0;
        SeedTasks();
        RefreshTasks();
    }

    private void SeedTasks()
    {
        _tasks.Add(new OperationTask("Prepare pending orders", "Pending", "High"));
        _tasks.Add(new OperationTask("Inspect dispatched tools", "Active", "Normal"));
        _tasks.Add(new OperationTask("Update low stock list", "Pending", "Normal"));
    }

    private async void OnAssignTaskClicked(object sender, EventArgs e)
    {
        var task = TaskEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(task))
        {
            await DisplayAlert("Task Needed", "Enter a task to assign.", "OK");
            return;
        }

        _tasks.Add(new OperationTask(task, "Pending", PriorityPicker.SelectedItem?.ToString() ?? "Normal"));
        TaskEntry.Text = string.Empty;
        RefreshTasks();
    }

    private async void OnMarkActiveClicked(object sender, EventArgs e) => await UpdateSelectedTaskAsync("Active");
    private async void OnCompleteTaskClicked(object sender, EventArgs e) => await UpdateSelectedTaskAsync("Completed");
    private void OnFilterChanged(object sender, EventArgs e) => RefreshTasks();

    private async Task UpdateSelectedTaskAsync(string status)
    {
        if (TaskPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Select Task", "Choose a task first.", "OK");
            return;
        }

        var visibleTasks = VisibleTasks().ToList();
        if (TaskPicker.SelectedIndex >= visibleTasks.Count)
        {
            return;
        }

        var selected = visibleTasks[TaskPicker.SelectedIndex];
        var sourceIndex = _tasks.FindIndex(task => task.Name == selected.Name && task.Priority == selected.Priority);
        if (sourceIndex >= 0)
        {
            _tasks[sourceIndex] = selected with { Status = status };
        }

        RefreshTasks();
    }

    private IEnumerable<OperationTask> VisibleTasks()
    {
        var filter = StatusFilterPicker.SelectedItem?.ToString() ?? "All";
        return filter == "All"
            ? _tasks
            : _tasks.Where(task => task.Status == filter);
    }

    private void RefreshTasks()
    {
        var visibleTasks = VisibleTasks().ToList();
        TaskPicker.ItemsSource = null;
        TaskPicker.ItemsSource = visibleTasks.Select(task => task.Name).ToList();
        WorkBoardLabel.Text = visibleTasks.Count == 0
            ? "No tasks for this filter."
            : string.Join(Environment.NewLine, visibleTasks.Select(task => $"{task.Name} - {task.Status} - {task.Priority}"));
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnCalendarClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Calendar());
    private async void OnDispatchClicked(object sender, EventArgs e) => await Navigation.PushAsync(new DispatchAndRequest());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());

    private sealed record OperationTask(string Name, string Status, string Priority);
}
