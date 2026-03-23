// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore.Identities;
using Cratis.Chronicle.Connections;
using Cratis.Execution;
using Cratis.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for adding the <see cref="IChronicleClient"/>.
/// </summary>
public static class ChronicleClientServiceCollectionExtensions
{
#if NET8_0
    static readonly object _eventStoreInitLock = new();
#else
    static readonly Lock _eventStoreInitLock = new();
#endif

    static readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStore> _eventStores = new();

    /// <summary>
    /// Add the <see cref="IChronicleClient"/> to the services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="chronicleBuilder">Optional <see cref="IChronicleBuilder"/> providing structural dependencies. When omitted, defaults are used.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleClient(this IServiceCollection services, IChronicleBuilder? chronicleBuilder = null)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IEventStoreNamespaceResolver>(sp =>
        {
            if (chronicleBuilder?.NamespaceResolver is not null)
            {
                return chronicleBuilder.NamespaceResolver;
            }

            var options = sp.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
            var resolverType = options.EventStoreNamespaceResolverType ?? throw new InvalidOperationException("EventStoreNamespaceResolverType cannot be null");

            if (!typeof(IEventStoreNamespaceResolver).IsAssignableFrom(resolverType))
            {
                throw new InvalidOperationException($"Type '{resolverType.FullName}' must implement IEventStoreNamespaceResolver");
            }

            return (IEventStoreNamespaceResolver)ActivatorUtilities.CreateInstance(sp, resolverType);
        });
        services.AddSingleton<IChronicleClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
            IChronicleConnection? connection = null;
            try
            {
                connection = sp.GetService<IChronicleConnection>();
            }
            catch { }

            var artifactsProvider = sp.GetRequiredService<IClientArtifactsProvider>();
            var identityProvider = chronicleBuilder?.IdentityProvider ?? new IdentityProvider(
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<ILogger<IdentityProvider>>());
            var namespaceResolver = sp.GetRequiredService<IEventStoreNamespaceResolver>();
            var correlationIdAccessor = sp.GetRequiredService<ICorrelationIdAccessor>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var namingPolicy = chronicleBuilder?.NamingPolicy ?? new DefaultNamingPolicy();

            return connection is null ?
                new ChronicleClient(options, artifactsProvider, sp, identityProvider, correlationIdAccessor, namespaceResolver, loggerFactory, namingPolicy) :
                new ChronicleClient(connection, options, artifactsProvider, sp, identityProvider, correlationIdAccessor, namespaceResolver, loggerFactory, namingPolicy);
        });

        services.AddScoped(sp =>
        {
            lock (_eventStoreInitLock)
            {
                var options = sp.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
                var namespaceResolver = sp.GetRequiredService<IEventStoreNamespaceResolver>();
                var namespaceName = namespaceResolver.Resolve();

                if (_eventStores.TryGetValue(namespaceName, out var eventStore))
                {
                    return eventStore;
                }

                var client = sp.GetRequiredService<IChronicleClient>();

                eventStore = client.GetEventStore(options.EventStore).GetAwaiter().GetResult();
                return _eventStores[namespaceName] = eventStore;
            }
        });

        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Constraints);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().EventLog);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().EventTypes);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Reactors);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Reducers);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Projections);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().ReadModels);

        services.AddSingleton<IClientArtifactsProvider>(_ => chronicleBuilder?.ClientArtifactsProvider ?? DefaultClientArtifactsProvider.Default);
        services.AddSingleton<INamingPolicy>(_ => chronicleBuilder?.NamingPolicy ?? new DefaultNamingPolicy());
        services.AddSingleton<ICorrelationIdAccessor>(_ => chronicleBuilder?.CorrelationIdAccessor ?? new CorrelationIdAccessor());

        return services;
    }
}
