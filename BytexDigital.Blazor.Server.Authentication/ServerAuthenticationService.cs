﻿using Microsoft.AspNetCore.Components.Authorization;

using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace BytexDigital.Blazor.Server.Authentication
{
    public class ServerAuthenticationService : IServerAuthenticationService
    {
        private readonly DelayedAuthenticationStateProvider _authenticationStateProvider;
        private readonly IPrincipalStorageProvider _orincipalStorageProvider;
        private readonly IPrincipalProvider _principalProvider;
        public static string USER_KEY = "USER.ID";

        public ServerAuthenticationService(
            AuthenticationStateProvider authenticationStateProvider,
            IPrincipalStorageProvider principalStorageProvider,
            IPrincipalProvider principalProvider)
        {
            _authenticationStateProvider = authenticationStateProvider as DelayedAuthenticationStateProvider;
            _orincipalStorageProvider = principalStorageProvider;
            _principalProvider = principalProvider;
        }

        private async Task<AuthenticationState> CreateClaimsPrincipalAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId != default)
                {
                    var principleClaims = await _principalProvider.CreateClaimsPrinipalAsync(userId, cancellationToken);

                    return new AuthenticationState(principleClaims);
                }
                else
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public string GetSignedInIdOrDefault()
        {
            return _orincipalStorageProvider.GetClaimsPrincipalOrDefault()?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task SignInAsAsync(string userId, bool setCookie = true, CancellationToken cancellationToken = default)
        {
            var authenticationState = await CreateClaimsPrincipalAsync(userId, cancellationToken);

            await _orincipalStorageProvider.SetClaimsPrincipalAsync(authenticationState.User, setCookie: setCookie, cancellationToken: cancellationToken);
            _authenticationStateProvider.SetAuthenticationStateTask(Task.FromResult(authenticationState));
        }

        public bool IsSignedIn()
        {
            ClaimsPrincipal claimsPrincipal = _orincipalStorageProvider.GetClaimsPrincipalOrDefault();

            return claimsPrincipal != null && claimsPrincipal.Identity.IsAuthenticated;
        }

        public async Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            await _orincipalStorageProvider.ClearClaimsPrinipalAsync(true, cancellationToken);
            _authenticationStateProvider.SetAuthenticationStateTask(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        public async Task InitializeFromCookiesAsync(CancellationToken cancellationToken = default)
        {
            ClaimsPrincipal claimsPrincipal = _orincipalStorageProvider.GetClaimsPrincipalOrDefault();

            if (claimsPrincipal != null)
            {
                _ = Task.Run(async () =>
                {
                    _authenticationStateProvider.SetAuthenticationStateTask(Task.FromResult(new AuthenticationState(claimsPrincipal)));
                    await _orincipalStorageProvider.SetClaimsPrincipalAsync(claimsPrincipal, setCookie: false, cancellationToken: cancellationToken);
                });
            }
            else
            {
                await _orincipalStorageProvider.ClearClaimsPrinipalAsync(false, cancellationToken);
                _authenticationStateProvider.SetAuthenticationStateTask(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
            }
        }
    }
}
