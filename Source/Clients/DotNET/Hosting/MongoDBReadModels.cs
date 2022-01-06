// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Configuration;
using Cratis.Configuration.Grains;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Strings;
using Cratis.Types;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans;

namespace Cratis.Hosting
{
    public static class MongoDBReadModels
    {
        static readonly MethodInfo _getCollectionMethod = typeof(IMongoDatabase).GetMethod(nameof(IMongoDatabase.GetCollection), BindingFlags.Public | BindingFlags.Instance)!;
        static readonly Dictionary<TenantId, IMongoDatabase> _databasesPerTenant = new();

        public static async Task ConfigureReadModels(IServiceProvider serviceProvider)
        {
            var configurations = serviceProvider.GetService<IClusterClient>()!.GetGrain<IConfigurations>(Guid.Empty);
            var storage = await configurations.GetStorage();
            var storageType = storage.Get(WellKnownStorageTypes.ReadModels);
            var clientFactory = serviceProvider.GetService<IMongoDBClientFactory>()!;

            foreach (var (tenant, config) in storageType.Tenants)
            {
                var url = new MongoUrl(config.ToString()!);
                var client = clientFactory.Create(url);
                _databasesPerTenant[tenant] = client.GetDatabase(url.DatabaseName);
            }
        }

        public static IServiceCollection AddMongoDBReadModels(this IServiceCollection services, ITypes types)
        {
            services.AddTransient(sp =>
            {
                var executionContext = sp.GetService<Execution.ExecutionContext>()!;
                return _databasesPerTenant[executionContext.TenantId];
            });

            var readModelTypes = types.All.SelectMany(_ => _
                .GetConstructors().SelectMany(c => c.GetParameters())
                .Where(_ =>
                    _.ParameterType.IsGenericType &&
                    _.ParameterType.IsAssignableTo(typeof(IMongoCollection<>).MakeGenericType(_.ParameterType.GetGenericArguments()[0]))))
                .Select(_ => _.ParameterType.GetGenericArguments()[0])
                .ToArray();

            foreach (var readModelType in readModelTypes)
            {
                services.AddTransient(typeof(IMongoCollection<>).MakeGenericType(readModelType), (sp) =>
                {
                    var database = sp.GetService<IMongoDatabase>();
                    var name = readModelType.Name.Pluralize();
                    var camelCaseName = name.ToCamelCase();
                    var genericMethod = _getCollectionMethod.MakeGenericMethod(readModelType);
                    return genericMethod.Invoke(database, new object[] { camelCaseName, null! })!;
                });
            }

            return services;
        }
    }
}
