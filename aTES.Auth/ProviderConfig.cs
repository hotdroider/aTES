using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace aTES.Auth
{
    /// <summary>
    /// Hardcoded popug structure for identity server
    /// </summary>
    public static class ProviderConfig
    {
        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("publickey", new [] {"publickey"}),
                new IdentityResource("roles", new[] { "role" })
            };


        public static IEnumerable<ApiResource> Apis =>
            new List<ApiResource>
            {  
                //no apis to protect for now, only some blazor web apps
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "aTES.Auth",
                    ClientName="aTES Authentification Service",
                    ClientSecrets = { new Secret("aTES.Auth".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = true,

                    RedirectUris = { "http://localhost:27627/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:27627/signout-callback-oidc" },

                    AllowedScopes = { "openid", "profile", "email", "roles", "publickey" },

                    AllowOfflineAccess = true
                },
                new Client
                {
                    ClientId = "aTES.Tasks",
                    ClientName="aTES Task Service",
                    ClientSecrets = { new Secret("aTES.Tasks".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = true,

                    RedirectUris = { "http://localhost:30478/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:30478/signout-callback-oidc" },

                    AllowedScopes = { "openid", "profile", "email", "roles", "publickey" },

                    AllowOfflineAccess = true
                },
                new Client
                {
                    ClientId = "aTES.Accounting",
                    ClientName="aTES Accounting Service",
                    ClientSecrets = { new Secret("aTES.Accounting".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = true,

                    RedirectUris = { "http://localhost:1645/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:1645/signout-callback-oidc" },

                    AllowedScopes = { "openid", "profile", "email", "roles", "publickey" },

                    AllowOfflineAccess = true
                },
                new Client
                {
                    ClientId = "aTES.Analytics",
                    ClientName="aTES Analytics Service",
                    ClientSecrets = { new Secret("aTES.Analytics".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = true,

                    RedirectUris = { "http://localhost:1643/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:1643/signout-callback-oidc" },

                    AllowedScopes = { "openid", "profile", "email", "roles", "publickey" },

                    AllowOfflineAccess = true
                }
            };   
    }
}