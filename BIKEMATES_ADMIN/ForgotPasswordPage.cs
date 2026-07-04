using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Controls.Shapes;

namespace BIKEMATES_ADMIN.Pages.Account;

public sealed class ForgotPasswordPage : ContentPage
{
    private const string Orange = "#FF6B00";
    private const string LightOrange = "#FFF3EA";
    private const string Dark = "#242424";
    private const string Muted = "#6E6E6E";
    private const string Line = "#E6E6E6";
    private string _email = "";
    private string _code = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string? _banner;
    private int _step;
    private bool _isBusy;

    public ForgotPasswordPage(string email = "")
    {
        Title = "Reset password";
        BackgroundColor = Colors.White;
        _email = email.Trim();
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(20, 28, 20, 28),
            Spacing = 14,
            BackgroundColor = Colors.White
        };

        body.Add(new Image { Source = "bikemate_logo", HeightRequest = 82, HorizontalOptions = LayoutOptions.Center });
        body.Add(Label("Reset password", 18, Dark, FontAttributes.Bold, TextAlignment.Center));
        body.Add(Label("We will send a six-digit code before changing the shop-admin password.", 13, Muted, FontAttributes.None, TextAlignment.Center));
        body.Add(StepRow());

        if (!string.IsNullOrWhiteSpace(_banner))
        {
            body.Add(Card(Label(_banner, 13, Muted)));
        }

        body.Add(_step switch
        {
            0 => EmailStep(),
            1 => CodeStep(),
            2 => PasswordStep(),
            _ => SuccessStep()
        });

        Content = new ScrollView { Content = body };
    }

    private View StepRow()
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(StepPill("1 Email", _step >= 0), 0, 0);
        grid.Add(StepPill("2 Code", _step >= 1), 1, 0);
        grid.Add(StepPill("3 Password", _step >= 2), 2, 0);
        return grid;
    }

    private View EmailStep()
    {
        var email = Input("Email address", _email, Keyboard.Email);
        email.TextChanged += (_, e) => _email = e.NewTextValue ?? "";

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Where should BikeMate send the code?", 13, Dark, FontAttributes.Bold));
        stack.Add(InputShell(email));
        stack.Add(PrimaryButton(_isBusy ? "Sending code..." : "Send reset code", SendCodeAsync));
        stack.Add(OutlineButton("Back to Login", async () => await Navigation.PopAsync()));
        return Card(stack);
    }

    private View CodeStep()
    {
        var code = Input("6-digit code", _code, Keyboard.Numeric, 6);
        code.TextChanged += (_, e) => _code = e.NewTextValue ?? "";

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label($"Enter the code sent to {_email}", 13, Dark, FontAttributes.Bold));
        stack.Add(InputShell(code));
        stack.Add(PrimaryButton(_isBusy ? "Checking code..." : "Verify code", VerifyCodeAsync));
        stack.Add(OutlineButton("Resend code", ResendCodeAsync));
        return Card(stack);
    }

    private View PasswordStep()
    {
        var password = Input("New password", _newPassword, Keyboard.Text);
        password.IsPassword = true;
        password.TextChanged += (_, e) => _newPassword = e.NewTextValue ?? "";

        var confirm = Input("Confirm new password", _confirmPassword, Keyboard.Text);
        confirm.IsPassword = true;
        confirm.TextChanged += (_, e) => _confirmPassword = e.NewTextValue ?? "";

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Create your new password", 13, Dark, FontAttributes.Bold));
        stack.Add(InputShell(password));
        stack.Add(InputShell(confirm));
        stack.Add(Label("Use more than 8 characters.", 11, Muted));
        stack.Add(PrimaryButton(_isBusy ? "Updating password..." : "Update password", ResetPasswordAsync));
        stack.Add(OutlineButton("Back to code", () =>
        {
            _step = 1;
            Render();
            return Task.CompletedTask;
        }));
        return Card(stack);
    }

    private View SuccessStep()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Password updated", 18, Dark, FontAttributes.Bold));
        stack.Add(Label("You can now sign in to the shop-admin app with your new password.", 13, Muted));
        stack.Add(PrimaryButton("Back to Login", async () => await Navigation.PopAsync()));
        return Card(stack);
    }

    private async Task SendCodeAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_email) || !_email.Contains('@', StringComparison.Ordinal) || !_email.Contains('.', StringComparison.Ordinal))
        {
            _banner = "Enter the email address on your BikeMate account.";
            Render();
            return;
        }

        await RunAsync(async () =>
        {
            await BikeMateDatabaseService.ForgotPasswordAsync(_email);
            _email = _email.Trim();
            _step = 1;
            _banner = "A six-digit reset code was sent. It expires in 15 minutes.";
        });
    }

    private async Task ResendCodeAsync()
    {
        await RunAsync(async () =>
        {
            await BikeMateDatabaseService.ResendPasswordResetOtpAsync(_email);
            _banner = "A new reset code was sent.";
        });
    }

    private async Task VerifyCodeAsync()
    {
        if (_code.Trim().Length != 6)
        {
            _banner = "Enter the six-digit code from your email.";
            Render();
            return;
        }

        await RunAsync(async () =>
        {
            await BikeMateDatabaseService.VerifyPasswordResetOtpAsync(_email, _code);
            _step = 2;
            _banner = "Code verified. Set your new shop-admin password.";
        });
    }

    private async Task ResetPasswordAsync()
    {
        if (_newPassword.Length <= 8)
        {
            _banner = "Use more than 8 characters for the new password.";
            Render();
            return;
        }

        if (!string.Equals(_newPassword, _confirmPassword, StringComparison.Ordinal))
        {
            _banner = "The new passwords do not match.";
            Render();
            return;
        }

        await RunAsync(async () =>
        {
            await BikeMateDatabaseService.ResetPasswordAsync(_email, _code, _newPassword, _confirmPassword);
            _banner = null;
            _step = 3;
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        _banner = null;
        Render();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _banner = ex.Message;
        }
        finally
        {
            _isBusy = false;
            Render();
        }
    }

    private static Label Label(string text, double size, string color, FontAttributes attributes = FontAttributes.None, TextAlignment alignment = TextAlignment.Start)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = attributes,
            FontFamily = attributes == FontAttributes.Bold ? "Inter" : "PublicSans",
            TextColor = Color.FromArgb(color),
            HorizontalTextAlignment = alignment
        };
    }

    private static Entry Input(string placeholder, string text, Keyboard keyboard, int maxLength = int.MaxValue)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Text = text,
            Keyboard = keyboard,
            MaxLength = maxLength,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb(Dark),
            PlaceholderColor = Color.FromArgb(Muted),
            FontSize = 13,
            FontFamily = "PublicSans"
        };
    }

    private static View InputShell(View content)
    {
        return new Border
        {
            Stroke = Color.FromArgb("#DCDCDC"),
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 2),
            Content = content
        };
    }

    private static View Card(View content)
    {
        return new Border
        {
            Stroke = Color.FromArgb(Line),
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(16),
            Content = content
        };
    }

    private static View StepPill(string text, bool active)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            BackgroundColor = Color.FromArgb(active ? LightOrange : "#F2F2F2"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 7),
            Content = Label(text, 11, active ? Orange : Muted, FontAttributes.Bold, TextAlignment.Center)
        };
    }

    private static Button PrimaryButton(string text, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(Orange),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontFamily = "Inter",
            FontSize = 13,
            CornerRadius = 8,
            HeightRequest = 48,
            Command = new Command(async () => await action())
        };
    }

    private static Button OutlineButton(string text, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb(Dark),
            BorderColor = Color.FromArgb("#DADCE0"),
            BorderWidth = 1,
            FontAttributes = FontAttributes.Bold,
            FontFamily = "Inter",
            FontSize = 13,
            CornerRadius = 8,
            HeightRequest = 46,
            Command = new Command(async () => await action())
        };
    }
}
