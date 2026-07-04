using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using BikeMate.Views.Auth;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BikeMate
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private int _currentPosition;
        private bool _isStartupVisible = true;
        private bool _isOnboardingVisible;
        private bool _isLoginVisible = false;
        private string _email = "";
        private string _password = "";
        private string _loginStatus = "Checking saved session...";

        public ObservableCollection<OnboardingItem> BoardingItems { get; set; }

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentButtonText));
            }
        }

        public string CurrentButtonText =>
            BoardingItems.Count == 0
                ? "Continue"
                : BoardingItems[Math.Clamp(CurrentPosition, 0, BoardingItems.Count - 1)].ButtonText;

        public bool IsStartupVisible
        {
            get => _isStartupVisible;
            set { _isStartupVisible = value; OnPropertyChanged(); }
        }

        public bool IsOnboardingVisible
        {
            get => _isOnboardingVisible;
            set { _isOnboardingVisible = value; OnPropertyChanged(); }
        }

        public bool IsLoginVisible
        {
            get => _isLoginVisible;
            set { _isLoginVisible = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string LoginStatus
        {
            get => _loginStatus;
            set { _loginStatus = value; OnPropertyChanged(); }
        }

        public ICommand NextCommand { get; }
        public ICommand SkipOnboardingCommand { get; }
        public ICommand SignInCommand { get; }
        public ICommand CreateAccountCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleLoginCommand { get; }

        public MainViewModel()
        {
            BoardingItems = new ObservableCollection<OnboardingItem>
            {
                new OnboardingItem
                {
                    Title = "BikeMate",
                    Description = "Book trusted motorcycle repair and roadside support from one place.",
                    ImageSource = "bikemate_logo",
                    ButtonText = "Continue"
                },
                new OnboardingItem
                {
                    Title = "Describe your concern",
                    Description = "Select your motorcycle, repair concern, service location, and preferred schedule.",
                    ImageSource = "bike_wrench.png",
                    ButtonText = "Continue"
                },
                new OnboardingItem
                {
                    Title = "Compare repair shops",
                    Description = "Review verified shops, available services, technicians, prices, and ratings before payment.",
                    ImageSource = "mechanic_door.png",
                    ButtonText = "Continue"
                },
                new OnboardingItem
                {
                    Title = "Track every step",
                    Description = "Message your service partners, follow repair progress, keep receipts, and review completed work.",
                    ImageSource = "running_man.png",
                    ButtonText = "Get started"
                }
            };

            NextCommand = new Command(GoToNextSlide);
            SkipOnboardingCommand = new Command(ShowLogin);

            SignInCommand = new Command(async () => await SignInAsync());
            CreateAccountCommand = new Command(async () => await OpenCreateAccountAsync());
            ForgotPasswordCommand = new Command(async () => await ForgotPasswordAsync());
            GoogleLoginCommand = new Command(async () => await LoginWithGoogleAsync());

            _ = InitializeStartupAsync();
        }

        private async Task InitializeStartupAsync()
        {
            try
            {
                await Task.Delay(100);
                if (Preferences.Default.Get(AppNavigation.ForceLoginPreferenceKey, false))
                {
                    Preferences.Default.Remove(AppNavigation.ForceLoginPreferenceKey);
                    ShowLogin();
                    LoginStatus = Preferences.Default.Get(
                        AppNavigation.LoginMessagePreferenceKey,
                        "You have been signed out.");
                    Preferences.Default.Remove(AppNavigation.LoginMessagePreferenceKey);
                    return;
                }

                var token = await SecureStorage.Default.GetAsync("access_token");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var sessionStatus = await ApiConfig.ValidateStoredSessionAsync();
                    if (sessionStatus == StoredSessionStatus.Valid)
                    {
                        var role = await SecureStorage.Default.GetAsync("primary_role");
                        await AppNavigation.NavigateByRoleAsync(string.IsNullOrWhiteSpace(role) ? AppRoles.Customer : role);
                        await PaymentReturnService.TryNavigateToCheckoutAsync();
                        return;
                    }

                    ShowLogin();
                    if (sessionStatus == StoredSessionStatus.Rejected)
                    {
                        AppNavigation.ClearSavedSession("Your saved session expired. Please sign in again.");
                        Preferences.Default.Remove(AppNavigation.ForceLoginPreferenceKey);
                        Preferences.Default.Remove(AppNavigation.LoginMessagePreferenceKey);
                        LoginStatus = "Your saved session expired. Please sign in again.";
                    }
                    else
                    {
                        ClearSavedSessionForLogin();
                        LoginStatus = "BikeMate could not verify your saved session, so it was cleared. Sign in again.";
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Startup session restore failed: {ex}");
            }

            IsStartupVisible = false;
            IsOnboardingVisible = true;
            IsLoginVisible = false;
            LoginStatus = "";
        }

        private void GoToNextSlide()
        {
            if (CurrentPosition < BoardingItems.Count - 1)
            {
                CurrentPosition++;
            }
            else
            {
                ShowLogin();
            }
        }

        private void ShowLogin()
        {
            IsStartupVisible = false;
            IsOnboardingVisible = false;
            IsLoginVisible = true;
            if (LoginStatus == "Checking saved session...")
            {
                LoginStatus = "";
            }
        }

        private async Task OpenCreateAccountAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Views.Auth.RegisterPage));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Create account", $"Could not open registration: {ex.Message}", "OK");
            }
        }

        private async Task ForgotPasswordAsync()
        {
            try
            {
                var route = string.IsNullOrWhiteSpace(Email)
                    ? nameof(Views.Auth.PasswordResetPage)
                    : $"{nameof(Views.Auth.PasswordResetPage)}?email={Uri.EscapeDataString(Email)}";
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Password reset", $"Could not open reset flow: {ex.Message}", "OK");
            }
        }

        private async Task LoginWithGoogleAsync()
        {
            try
            {
                LoginStatus = "Opening Google sign-in...";
                var auth = await GoogleSignInService.SignInAsync(AppRoles.Customer);
                await GoogleSignInService.StoreAuthAsync(auth);
                var role = GoogleSignInService.PickPrimaryRole(auth.User.Roles);
                if (role == AppRoles.Customer && await ShouldOpenGoogleCustomerSetupAsync(auth))
                {
                    LoginStatus = "Google sign-in complete. Opening account setup...";
                    await NavigateToGoogleSetupAsync();
                    return;
                }

                await AppNavigation.NavigateByRoleAsync(role);
            }
            catch (TaskCanceledException)
            {
                LoginStatus = "Google sign-in was cancelled.";
            }
            catch (Exception ex)
            {
                LoginStatus = "Google sign-in failed.";
                await Shell.Current.DisplayAlertAsync("Google sign-in failed", ex.Message, "OK");
            }
        }

        private async Task SignInAsync()
        {
            try
            {
                using var http = ApiConfig.CreateHttpClient();
                var response = await http.PostAsJsonAsync("auth/login", new LoginRequestDto(Email, Password));

                if (response.IsSuccessStatusCode)
                {
                    var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (auth is not null)
                    {
                        var role = PickPrimaryRole(auth.User.Roles);
                        await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                        await SecureStorage.Default.SetAsync("primary_role", role);
                        await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
                        await AppNavigation.NavigateByRoleAsync(role);
                        return;
                    }
                }

                var error = await response.Content.ReadAsStringAsync();
                LoginStatus = string.IsNullOrWhiteSpace(error)
                    ? "Wrong email or password."
                    : error;
                await Shell.Current.DisplayAlertAsync("Sign in failed", LoginStatus, "OK");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sign-in failed: {ex}");
                LoginStatus = "Could not reach BikeMate right now. Check your internet connection and try again.";
                await Shell.Current.DisplayAlertAsync("Sign in unavailable", LoginStatus, "OK");
            }
        }

        private static void ClearSavedSessionForLogin()
        {
            SecureStorage.Default.Remove("access_token");
            SecureStorage.Default.Remove("primary_role");
            SecureStorage.Default.Remove("user_id");
            Preferences.Default.Remove(AppNavigation.ForceLoginPreferenceKey);
            Preferences.Default.Remove(AppNavigation.LoginMessagePreferenceKey);
        }

        private static async Task<bool> ShouldOpenGoogleCustomerSetupAsync(AuthResponseDto auth)
        {
            if (string.Equals(auth.User.AccountStatus, "pending", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                var customer = await CustomerApiClient.GetCustomerAsync();
                return IsGoogleCustomerSetupIncomplete(customer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Google customer setup check failed: {ex}");
                return true;
            }
        }

        private static bool IsGoogleCustomerSetupIncomplete(CustomerMeDto customer)
        {
            var address = customer.Addresses.FirstOrDefault(item => item.IsDefault) ?? customer.Addresses.FirstOrDefault();
            var motorcycle = customer.Motorcycles.FirstOrDefault();

            return string.IsNullOrWhiteSpace(customer.PhoneNumber) ||
                string.IsNullOrWhiteSpace(customer.Sex) ||
                customer.Birthdate is null ||
                string.IsNullOrWhiteSpace(customer.ValidIdImageUrl) ||
                address is null ||
                string.IsNullOrWhiteSpace(address.AddressLine) ||
                string.IsNullOrWhiteSpace(address.City) ||
                string.IsNullOrWhiteSpace(address.Province) ||
                string.IsNullOrWhiteSpace(address.PostalCode) ||
                motorcycle is null ||
                string.IsNullOrWhiteSpace(motorcycle.Brand) ||
                string.IsNullOrWhiteSpace(motorcycle.Model) ||
                string.IsNullOrWhiteSpace(motorcycle.PlateNumber);
        }

        private static async Task NavigateToGoogleSetupAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Yield();

                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page ??= new AppShell();
                }

                if (Shell.Current is not null)
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(GoogleAccountSetupPage));
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Google setup route failed: {ex}");
                        await Shell.Current.Navigation.PushAsync(new GoogleAccountSetupPage());
                        return;
                    }
                }

                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new NavigationPage(new GoogleAccountSetupPage());
                }
            });
        }

        private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
        {
            if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
            if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
            if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
            return AppRoles.Customer;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }
    }
