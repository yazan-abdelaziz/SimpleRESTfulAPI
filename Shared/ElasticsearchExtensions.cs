using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;

namespace Shared
{
    public static class ElasticsearchExtensions
    {
        public static IServiceCollection AddElasticsearch(this IServiceCollection services)
        {
            var url = "http://localhost:9200/";
            var defaultIndex = "employee";

            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex(defaultIndex);

            AddDefaultMappings(settings);

            var client = new ElasticClient(settings);

            services.AddSingleton(client);

            CreateIndex(client, defaultIndex);
            return services;
        }

        private static void AddDefaultMappings(ConnectionSettings settings)
        {
            
                settings.DefaultMappingFor<Employee>(m => m.IdProperty(p => p.EmployeeId));
        }

        private static void CreateIndex(IElasticClient client, string indexName)
        {
            var createIndexResponse = client.Indices.Create(indexName,
                index => index.Map<Employee>(x => x.AutoMap())
            );
        }
    }
}
