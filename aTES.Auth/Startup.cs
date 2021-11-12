using aTES.Auth.Data;
using aTES.Auth.Services;
using aTES.Blazor;
using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace aTES.Auth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddPopugAuthentification(Configuration);

            //setup asp identity for popugs
            var connectionString = Configuration.GetConnectionString("UserStoreConnection");
            services.AddDbContext<PopugsDbContext>(config => config.UseSqlServer(connectionString));

            services.AddIdentity<PopugUser, IdentityRole>(config =>
            {
                config.Password.RequiredLength = 4;
                config.Password.RequireDigit = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            }).AddEntityFrameworkStores<PopugsDbContext>()
              .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(c =>
            {
                c.Cookie.Name = "aTES.AuthCookie";
                c.LoginPath = "/Accounts/Login/";
                c.LogoutPath = "/Accounts/Logout/";
            });

            services.AddControllersWithViews();

            // configures IIS out-of-proc settings (see https://github.com/aspnet/AspNetCore/issues/14882)
            services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            // configures IIS in-proc settings
            services.Configure<IISServerOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            //setup popug identity server
            services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
                .AddInMemoryIdentityResources(ProviderConfig.Ids)
                .AddInMemoryApiResources(ProviderConfig.Apis)
                .AddInMemoryClients(ProviderConfig.Clients)
                .AddAspNetIdentity<PopugUser>()
                .AddProfileService<PopugProfileService>() //claims issuer
                .AddDeveloperSigningCredential();

            services.AddPopugBlazor();

            services.AddPopugEventSchemas(Configuration);

            services.AddSingleton<IProducer>(s =>
            {
                var brokers = Configuration.GetSection("Kafka:Brokers").Get<string[]>();
                return new CommonProducer(brokers);
            });

            services.AddScoped<IAccountService, IdentityAccountService>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
