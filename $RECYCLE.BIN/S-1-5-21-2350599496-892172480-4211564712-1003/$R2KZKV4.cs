using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;
using System.Text.Json;
using WebDataProject.Data.Models;

namespace WebDataProject.Web;

public class ServerCookieAuthenticationStateProvider(HttpClient HttpClient, NavigationManager navigationManager) : ServerAuthenticationStateProvider, IDisposable
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var userResponse = await HttpClient.GetAsync("manage/info");
        if (userResponse.IsSuccessStatusCode)
        {
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<User>(userJson);
            if (userInfo != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Email, userInfo.Email ?? "")
                };
                var id = new ClaimsIdentity(claims, nameof(ServerCookieAuthenticationStateProvider));
                user = new ClaimsPrincipal(id);
            }
        }

        return new AuthenticationState(user);
    }

    public void Login(string email, string password)
        => NotifyAuthenticationStateChanged(LoginAndGetAuthenticationStateAsync(email, password));
    public async Task<AuthenticationState> LoginAndGetAuthenticationStateAsync(string email, string password)
    {
        _ = await HttpClient.PostAsJsonAsync(
            "login?useCookies=true", new
            {
                email,
                password
            });

        return await GetAuthenticationStateAsync();
    }

    public void Dispose() => HttpClient.Dispose();
}
