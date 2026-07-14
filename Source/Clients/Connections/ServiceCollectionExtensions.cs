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
    /// <param name="connectionString">The Chronicle URL to connect to. If not provided, defaults to <see cref="ChronicleConnectionString.Default"/>.</param>
    /// <param name="connectionStringFactory">A factory function to create the Chronicle URL. If provided, it will be used to determine the URL instead of the default.</param>
    /// <param name="disableTls">Whether to disable TLS for the connection.</param>
    /// <param name="certificatePath">Path to the TLS certificate file.</param>
    /// <param name="certificatePassword">Password for the TLS certificate file.</param>
    /// <param name="skipCompatibilityCheck">Whether to skip the server compatibility check on connect. Useful for short-lived clients such as CLIs.</param>
    /// <param name="skipKeepAlive">Whether to skip the keep-alive handshake on connect. Useful for short-lived clients such as CLIs.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// If the <paramref name="connectionString"/> is not specified, it will use the <paramref name="connectionStringFactory"/> if specified, if not, it defaults to <see cref="ChronicleConnectionString.Default"/>.
    /// </remarks>
    public static IServiceCollection AddCratisChronicleConnection(
        this IServiceCollection services,
        ChronicleConnectionString? connectionString = default,
        Func<IServiceProvider, ChronicleConnectionString>? connectionStringFactory = default,
        bool? disableTls = null,
        string? certificatePath = null,
        string? certificatePassword = null,
        bool skipCompatibilityCheck = false,
        bool skipKeepAlive = false)
    {
        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddSingleton<IChronicleConnection>(sp =>
        {
            connectionString ??= connectionStringFactory?.Invoke(sp) ?? ChronicleConnectionString.Default;
            disableTls ??= connectionString.DisableTls;
            var logger = sp.GetService<ILogger<ChronicleConnection>>();
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger?.LogInformation("Configuring Chronicle connection with connection string: {ConnectionString}", connectionString);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
            var connectionLifecycle = new ConnectionLifecycle(sp.GetRequiredService<ILogger<ConnectionLifecycle>>());
            var correlationIdAccessor = sp.GetRequiredService<ICorrelationIdAccessor>();

            // Authenticate with the server using the connection string's client credentials - the
            // keep-alive and service calls are rejected without a bearer token.
            ITokenProvider tokenProvider = new NoOpTokenProvider();
            if (connectionString.AuthenticationMode == AuthenticationMode.ClientCredentials)
            {
                var username = connectionString.Username;
                var password = connectionString.Password;
                if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
                {
                    username = ChronicleConnectionString.DevelopmentClient;
                    password = ChronicleConnectionString.DevelopmentClientSecret;
                }

                tokenProvider = new OAuthTokenProvider(
                    () => sp.GetService<IChronicleConnection>() is ChronicleConnection currentConnection
                        ? currentConnection.CurrentServerAddress
                        : connectionString.ServerAddress,
                    username!,
                    password!,
                    disableTls.Value,
                    sp.GetRequiredService<ILogger<OAuthTokenProvider>>());
            }

            return new ChronicleConnection(
                connectionString,
                5,
                null,
                null,
                connectionLifecycle,
                new Cratis.Tasks.TaskFactory(),
                correlationIdAccessor,
                sp.GetRequiredService<ILoggerFactory>(),
                lifetime.ApplicationStopping,
                sp.GetRequiredService<ILogger<ChronicleConnection>>(),
                disableTls.Value,
                certificatePath,
                certificatePassword,
                tokenProvider,
                skipCompatibilityCheck: skipCompatibilityCheck,
                skipKeepAlive: skipKeepAlive);
        });

        // Deliberately transient: the connection recreates its service proxies on every
        // (re)connect - a failover to another server disposes the old gRPC channel. Caching the
        // proxies as singletons would leave every consumer on a dead channel after a reconnect.
        services.AddTransient(sp =>
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
        services.AddTransient(sp => sp.GetRequiredService<IServices>().EventStores);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Namespaces);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Recommendations);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Identities);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().EventSequences);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().EventTypes);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Constraints);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Observers);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().FailedPartitions);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Reactors);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Reducers);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Projections);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().ReadModels);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Jobs);
        services.AddTransient(sp => sp.GetRequiredService<IServices>().Connections);

        return services;
    }
}
