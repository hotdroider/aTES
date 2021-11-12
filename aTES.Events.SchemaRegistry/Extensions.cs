using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace aTES.Events.SchemaRegistry
{
    public static class Extensions
    {
        public static void AddPopugEventSchemas(this IServiceCollection services, IConfiguration configuration)
        {
            var schemaPaths = configuration.GetValue<string>("SchemasFolder");
            schemaPaths = schemaPaths ?? Environment.CurrentDirectory;

            services.AddSingleton(s => new SchemaRepository(schemaPaths));
            services.AddSingleton<SchemaRegistry>();
        }
    }
}

