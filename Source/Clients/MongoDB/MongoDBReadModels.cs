// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Aksio.Cratis;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Models;
using Aksio.Cratis.MongoDB;
using Aksio.Execution;
using Aksio.MongoDB;
using Aksio.Reflection;
using Aksio.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

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
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="readModelTypeProvider">Optional <see cref="ICanProvideMongoDBReadModelTypes"/> for providing the read model types. Will default to <see cref="DefaultMongoDBReadModelTypesProvider"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddMongoDBReadModels(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = default,
        ICanProvideMongoDBReadModelTypes? readModelTypeProvider = default)
    {
        #pragma warning disable CA2000 // Dispose objects before losing scope - Logger factory will be disposed when process exits
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("MongodBReadModels");

        readModelTypeProvider ??= new DefaultMongoDBReadModelTypesProvider(ProjectReferencedAssemblies.Instance);

        services.AddTransient(sp =>
        {
            var executionContext = sp.GetService<Aksio.Execution.ExecutionContext>()!;
            lock (_databasesPerTenant)
            {
                if (_databasesPerTenant.IsEmpty)
                {
                    ConfigureReadModels(sp).Wait();
                }
            }
            return _databasesPerTenant[executionContext.TenantId];
        });

        var readModelTypes = readModelTypeProvider.Provide();
        RegisterMongoCollectionTypes(services, readModelTypes, logger);
        return services;
    }

    static string GetReadModelName(IModelNameConvention modelNameConvention, Type readModelType)
    {
        if (readModelType.HasAttribute<ModelNameAttribute>())
        {
            var modelNameAttribute = readModelType.GetCustomAttribute<ModelNameAttribute>()!;
            return modelNameAttribute.Name;
        }

        return modelNameConvention.GetNameFor(readModelType);
    }

    static void RegisterMongoCollectionTypes(IServiceCollection services, IEnumerable<Type> readModelTypes, ILogger logger)
    {
        var modelNameConvention = services.BuildServiceProvider().GetService<IModelNameConvention>() ?? new DefaultModelNameConvention();
        foreach (var readModelType in readModelTypes)
        {
            var modelName = GetReadModelName(modelNameConvention, readModelType);

            logger.AddingMongoDBCollectionBinding(readModelType, modelName);
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
        var storage = await serviceProvider.GetRequiredService<IMicroserviceConfiguration>().Storage();
        var clientFactory = serviceProvider.GetRequiredService<IMongoDBClientFactory>();
        foreach (var (tenant, config) in storage.Tenants)
        {
            var storageType = config.Get(WellKnownStorageTypes.ReadModels);
            var url = new MongoUrl(storageType.ConnectionDetails.ToString()!);
            var client = clientFactory.Create(url);
            _databasesPerTenant[tenant] = client.GetDatabase(url.DatabaseName);
        }
    }
}
