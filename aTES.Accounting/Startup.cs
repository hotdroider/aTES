using aTES.Accounting.Data;
using aTES.Accounting.Services;
using aTES.Blazor;
using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aTES.Accounting
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

            var connectionString = Configuration.GetConnectionString("AccountingStoreConnection");
            services.AddDbContext<AccountingDbContext>(config => config.UseSqlServer(connectionString));


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

            services.AddScoped<BillingService>();

            services.AddHostedService<AccountsUpdater>();
            services.AddHostedService<TaskUpdater>();
            services.AddHostedService<BillingProcessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
