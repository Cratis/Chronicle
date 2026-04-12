// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Identities;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for adding the Chronicle client services to an <see cref="IServiceCollection"/> for non-ASP.NET Core hosts.
/// </summary>
/// <remarks>
/// This extension is intended for use with worker services and other non-web hosts. For ASP.NET Core web applications,
/// use the <c>AddCratisChronicleClient</c> extension in the <c>Microsoft.AspNetCore.Builder</c> namespace instead,
/// which provides ASP.NET Core–specific defaults (HTTP header–based namespace resolution, HTTP context identity provider).
/// </remarks>
internal static class ChronicleClientServiceCollectionExtensions
{
#if NET8_0
    static readonly object _eventStoreInitLock = new();
#else
    static readonly Lock _eventStoreInitLock = new();
#endif

    static readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStore> _eventStores = new();

    /// <summary>
    /// Add the Chronicle client services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="chronicleBuilder">Optional <see cref="IChronicleBuilder"/> providing structural dependencies. When omitted, defaults are used.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleClient(this IServiceCollection services, IChronicleBuilder? chronicleBuilder = null)
    {
        services.AddSingleton(sp =>
        {
            if (chronicleBuilder?.NamespaceResolver is not null)
            {
                return chronicleBuilder.NamespaceResolver;
            }

            var options = sp.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
            var resolverType = options.EventStoreNamespaceResolverType ?? typeof(DefaultEventStoreNamespaceResolver);

            if (!typeof(IEventStoreNamespaceResolver).IsAssignableFrom(resolverType))
            {
                throw new InvalidOperationException($"Type '{resolverType.FullName}' must implement IEventStoreNamespaceResolver");
            }

            return (IEventStoreNamespaceResolver)ActivatorUtilities.CreateInstance(sp, resolverType);
        });

        services.AddSingleton<IChronicleClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
            var connection = sp.GetService<IChronicleConnection>();

            var artifactsProvider = sp.GetRequiredService<IClientArtifactsProvider>();
            var identityProvider = chronicleBuilder?.IdentityProvider
                ?? sp.GetService<IIdentityProvider>()
                ?? new BaseIdentityProvider();
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
                var options = sp.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
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

        services.AddSingleton(_ => chronicleBuilder?.ClientArtifactsProvider ?? DefaultClientArtifactsProvider.Default);
        services.AddSingleton(_ => chronicleBuilder?.NamingPolicy ?? new DefaultNamingPolicy());
        services.AddSingleton(_ => chronicleBuilder?.CorrelationIdAccessor ?? new CorrelationIdAccessor());

        return services;
    }
}
