// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for adding the <see cref="IChronicleClient"/>.
/// </summary>
public static class ChronicleClientServiceCollectionExtensions
{
    static readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStore> _eventStores = new();

    /// <summary>
    /// Add the <see cref="IChronicleClient"/> to the services.
    /// </summary>
    /// /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddChronicleClient(this IServiceCollection services)
    {
        services.AddSingleton<IChronicleClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
            var chronicleOptions = new ChronicleOptions(options.Url)
            {
                ServiceProvider = sp,
                SoftwareVersion = options.SoftwareVersion,
                SoftwareCommit = options.SoftwareCommit,
                ProgramIdentifier = options.ProgramIdentifier
            };
            return new ChronicleClient(chronicleOptions);
        });

        services.AddScoped(sp =>
        {
            var namespaceName = EventStoreNamespaceName.Default;

            var options = sp.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
            if (sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers.TryGetValue(options.NamespaceHttpHeader, out var values) ?? false)
            {
                namespaceName = values.ToString();
            }

            if (_eventStores.TryGetValue(namespaceName, out var eventStore))
            {
                return eventStore;
            }

            var client = sp.GetRequiredService<IChronicleClient>();
            eventStore = client.GetEventStore(options.EventStore, namespaceName);
            return _eventStores[namespaceName] = eventStore;
        });

        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Constraints);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().EventLog);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().EventTypes);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Reactors);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Reducers);
        services.AddScoped(sp => sp.GetRequiredService<IEventStore>().Projections);
        services.AddSingleton(sp => sp.GetRequiredService<IChronicleClient>().Options.ArtifactsProvider);
        services.AddSingleton(sp => sp.GetRequiredService<IChronicleClient>().Options.ModelNameConvention);
        services.AddSingleton(sp => sp.GetRequiredService<IChronicleClient>().Options.CorrelationIdAccessor);

        return services;
    }
}
