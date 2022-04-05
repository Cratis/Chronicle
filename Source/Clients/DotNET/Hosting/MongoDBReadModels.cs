// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Configuration.Grains;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Models;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Strings;
using Aksio.Cratis.Types;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans;

namespace Aksio.Cratis.Hosting;

/// <summary>
/// Extension methods for configuring MongoDB based read models.
/// </summary>
public static class MongoDBReadModels
{
    static readonly MethodInfo _getCollectionMethod = typeof(IMongoDatabase).GetMethod(nameof(IMongoDatabase.GetCollection), BindingFlags.Public | BindingFlags.Instance)!;
    static readonly ConcurrentDictionary<TenantId, IMongoDatabase> _databasesPerTenant = new();

    /// <summary>
    /// Add all services related to being able to use MongoDB for read models.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddMongoDBReadModels(this IServiceCollection services, ITypes types, ILoggerFactory? loggerFactory = default)
    {
        var logger = loggerFactory?.CreateLogger("MongodBReadModels");

        services.AddTransient(sp =>
        {
            var executionContext = sp.GetService<Execution.ExecutionContext>()!;
            lock (_databasesPerTenant)
            {
                if (_databasesPerTenant.IsEmpty)
                {
                    ConfigureReadModels(sp).Wait();
                }
            }
            return _databasesPerTenant[executionContext.TenantId];
        });

        var readModelTypes = GetMongoCollections(types).ToList();
        readModelTypes.AddRange(GetProvidersForMongoCollections(types, readModelTypes));
        RegisterMongoCollectionTypes(services, readModelTypes, logger);

        return services;
    }

    static void RegisterMongoCollectionTypes(IServiceCollection services, IEnumerable<Type> readModelTypes, ILogger? logger = default)
    {
        foreach (var readModelType in readModelTypes)
        {
            var modelName = readModelType.Name;
            if (readModelType.HasAttribute<ModelNameAttribute>())
            {
                var modelNameAttribute = readModelType.GetCustomAttribute<ModelNameAttribute>()!;
                modelName = modelNameAttribute.Name;
            }
            else
            {
                modelName = readModelType.Name.Pluralize();
                modelName = modelName.ToCamelCase();
            }

            logger?.AddingMongoDBCollectionBinding(readModelType, modelName);
            services.AddTransient(typeof(IMongoCollection<>).MakeGenericType(readModelType), (sp) =>
            {
                var database = sp.GetService<IMongoDatabase>();
                var genericMethod = _getCollectionMethod.MakeGenericMethod(readModelType);
                return genericMethod.Invoke(database, new object[] { modelName, null! })!;
            });
        }
    }

    static async Task ConfigureReadModels(IServiceProvider serviceProvider)
    {
        var configurations = serviceProvider.GetService<IClusterClient>()!.GetGrain<IConfigurations>(Guid.Empty);
        var storage = await configurations.GetStorage();
        var clientFactory = serviceProvider.GetService<IMongoDBClientFactory>()!;

        foreach (var (tenant, config) in storage.Tenants)
        {
            var storageType = config.Get(WellKnownStorageTypes.ReadModels);
            var url = new MongoUrl(storageType.ConnectionDetails.ToString()!);
            var client = clientFactory.Create(url);
            _databasesPerTenant[tenant] = client.GetDatabase(url.DatabaseName);
        }
    }

    static IEnumerable<Type> GetMongoCollections(ITypes types) => types.All.SelectMany(_ => _
            .GetConstructors().SelectMany(c => c.GetParameters())
            .Where(_ =>
                _.ParameterType.IsGenericType && IsMongoCollection(_.ParameterType)))
            .Select(_ => _.ParameterType.GetGenericArguments()[0]);

    static IEnumerable<Type> GetProvidersForMongoCollections(ITypes types, IEnumerable<Type> typesToSkip) => types.All.Except(typesToSkip).SelectMany(_ => _
            .GetConstructors().SelectMany(c => c.GetParameters())
            .Where(_ =>
                _.ParameterType.IsGenericType &&
                _.ParameterType.GetGenericArguments()[0].IsGenericType &&
                IsProviderForMongoCollection(_.ParameterType)))
            .Select(_ => _.ParameterType.GetGenericArguments()[0].GetGenericArguments()[0]);

    static bool IsMongoCollection(Type type) => type.IsAssignableTo(typeof(IMongoCollection<>).MakeGenericType(type.GetGenericArguments()[0]));

    static bool IsProviderForMongoCollection(Type type) => type.IsAssignableTo(typeof(ProviderFor<>).MakeGenericType(typeof(IMongoCollection<>).MakeGenericType(type.GetGenericArguments()[0].GetGenericArguments()[0])));
}
