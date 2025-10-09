using Microsoft.AspNetCore.Components.Authorization;
using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Client.Features.Authentication.Services
{
    /// <summary>
    /// Custom AuthenticationStateProvider that integrates with our API key authentication
    /// </summary>
    public class ApiKeyAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly AuthenticationStateService _authService;

        public ApiKeyAuthenticationStateProvider(AuthenticationStateService authService)
        {
            _authService = authService;
            _authService.AuthenticationStateChanged += NotifyAuthenticationStateChanged;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = _authService.CurrentUser;
            
            if (user is null || !_authService.IsAuthenticated)
            {
                return Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, user.Username),
                new(System.Security.Claims.ClaimTypes.Email, user.Email),
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("user_id", user.Id.ToString())
            };

            // Add roles as separate claims
            foreach (var role in user.Roles)
            {
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
            }

            // Make sure the identity is marked as authenticated with the correct authentication type
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "apikey", System.Security.Claims.ClaimTypes.Name, System.Security.Claims.ClaimTypes.Role);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            return Task.FromResult(new AuthenticationState(principal));
        }

        private void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void Dispose()
        {
            _authService.AuthenticationStateChanged -= NotifyAuthenticationStateChanged;
        }
    }
}