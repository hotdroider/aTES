using aTES.Blazor;
using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using aTES.Tasks.Data;
using aTES.Tasks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aTES.Tasks
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

            var connectionString = Configuration.GetConnectionString("TaskStoreConnection");
            services.AddDbContext<TasksDbContext>(config => config.UseSqlServer(connectionString));

            services.AddScoped<TaskService>();

            services.AddPopugEventSchemas(Configuration);
            services.AddLogging(c =>
            {
                c.ClearProviders();
                c.AddConsole();
            });

            var logger = services.BuildServiceProvider().GetService<ILogger<Startup>>();

            var kafkaBrokers = Configuration.GetSection("Kafka:Brokers").Get<string[]>();
            services.AddSingleton<IProducer>(s => new CommonProducer(logger, kafkaBrokers, FailoverPolicy.WithRetry(3)));
            services.AddSingleton<IConsumerFactory>(s => new ConsumerFactory(kafkaBrokers, logger));

            services.AddPopugBlazor();

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddHostedService<AccountsUpdater>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
