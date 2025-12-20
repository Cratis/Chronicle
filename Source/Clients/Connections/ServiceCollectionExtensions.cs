// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add Chronicle services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Chronicle connection to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="url">The Chronicle URL to connect to. If not provided, defaults to <see cref="ChronicleUrl.Default"/>.</param>
    /// <param name="urlFactory">A factory function to create the Chronicle URL. If provided, it will be used to determine the URL instead of the default.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// If the <paramref name="url"/> is not specified, it will use the <paramref name="urlFactory"/> if specified, if not, it defaults to <see cref="ChronicleUrl.Default"/>.
    /// </remarks>
    public static IServiceCollection AddCratisChronicleConnection(this IServiceCollection services, ChronicleUrl? url = default, Func<IServiceProvider, ChronicleUrl>? urlFactory = default)
    {
        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddSingleton<IChronicleConnection>(sp =>
        {
            url ??= urlFactory?.Invoke(sp) ?? ChronicleUrl.Default;

            var logger = sp.GetService<ILogger<ChronicleConnection>>();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger?.LogInformation("Configuring Chronicle connection with connection string: {ConnectionString}", url);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
            var connectionLifecycle = new ConnectionLifecycle(sp.GetRequiredService<ILogger<ConnectionLifecycle>>());
            var correlationIdAccessor = sp.GetRequiredService<ICorrelationIdAccessor>();
            return new ChronicleConnection(
                url,
                5,
                null,
                null,
                connectionLifecycle,
                new Cratis.Tasks.TaskFactory(),
                correlationIdAccessor,
                sp.GetRequiredService<ILogger<ChronicleConnection>>(),
                lifetime.ApplicationStopping);
        });

        services.AddSingleton(sp =>
        {
            var connection = (sp.GetRequiredService<IChronicleConnection>() as IChronicleServicesAccessor)!;
            return connection.Services;
        });

        return services;
    }

    /// <summary>
    /// Adds the Chronicle services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleServices(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventStores);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Namespaces);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Recommendations);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Identities);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventSequences);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventTypes);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Constraints);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Observers);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().FailedPartitions);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Reactors);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Reducers);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Projections);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().ReadModels);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Jobs);

        return services;
    }
}
