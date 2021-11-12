using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace aTES.Common
{
    public static class Extensions
    {
        public static AuthenticationBuilder AddPopugAuthentification(this IServiceCollection services, IConfiguration config)
        {
            return services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
               .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddPopugOpenId(config);
        }

        public static AuthenticationBuilder AddPopugOpenId(this AuthenticationBuilder builder, IConfiguration config)
        {
            return builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,
                   options =>
                   {
                       options.Authority = config.GetValue<string>("PopugOpenIdAuth:Authority");
                       options.ClientId = config.GetValue<string>("PopugOpenIdAuth:ClientId");
                       options.ClientSecret = config.GetValue<string>("PopugOpenIdAuth:ClientSecret");
                       options.UsePkce = true;
                       options.ResponseType = "code";
                       options.Scope.Add("openid");
                       options.Scope.Add("profile");
                       options.Scope.Add("email");
                       options.Scope.Add("publickey");
                       options.ClaimActions.MapUniqueJsonKey("publickey", "publickey");
                       options.RequireHttpsMetadata = false;

                       options.TokenValidationParameters.NameClaimType = "name";
                       options.TokenValidationParameters.RoleClaimType = "role";

                       options.SaveTokens = true;
                       options.GetClaimsFromUserInfoEndpoint = true;
                   });
        }
    }
}
