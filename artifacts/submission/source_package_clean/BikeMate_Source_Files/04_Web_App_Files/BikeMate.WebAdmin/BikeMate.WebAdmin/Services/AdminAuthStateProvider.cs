using BikeMate.WebAdmin;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace BikeMate.WebAdmin.Services;

public class AdminAuthStateProvider : AuthenticationStateProvider
{
    private readonly AdminApiClient _adminApi;
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public AdminAuthStateProvider(AdminApiClient adminApi, ProtectedSessionStorage sessionStorage)
    {
        _adminApi = adminApi;
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSession = await _sessionStorage.GetAsync<string>("admin_email");

            if (userSession.Success && !string.IsNullOrEmpty(userSession.Value))
            {
                var claims = new[] {
                    new Claim(ClaimTypes.Name, userSession.Value),
                    new Claim(ClaimTypes.Role, "SystemAdmin")
                };
                var identity = new ClaimsIdentity(claims, "BlazorAuth");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch (Exception)
        {
            // CRITICAL FIX: Catching ALL exceptions prevents the yellow bar crash 
            // if the browser storage is blocked or keys are missing.
        }

        return new AuthenticationState(_anonymous);
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        bool isValid = await _adminApi.ValidateAdminLoginAsync(email, password);

        if (isValid)
        {
            await _sessionStorage.SetAsync("admin_email", email);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return true;
        }
        return false;
    }

    public async Task LogoutAsync()
    {
        try { await _sessionStorage.DeleteAsync("admin_email"); } catch { }
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}