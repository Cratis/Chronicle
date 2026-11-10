// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Builds a role-specific Chronicle workload host.
/// </summary>
internal static class WorkloadHost
{
    /// <summary>
    /// Creates a host with only the artifacts required by its role.
    /// </summary>
    /// <param name="role">The worker role.</param>
    /// <param name="connection">The kernel connection string.</param>
    /// <param name="store">The event store name.</param>
    /// <param name="sideLedger">The reactor delivery ledger.</param>
    /// <returns>The caller-owned host.</returns>
    public static IHost Create(string role, string connection, string store, SideLedger sideLedger)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.AddCratisChronicle(
            options =>
            {
                options.ConnectionString = new(connection);
                options.EventStore = store;
                options.ActivationScope = ActivationScope.PerProcess;
                options.AutoDiscoverAndRegister = role != "reader";
            },
            configure: chronicle => chronicle.ClientArtifactsProvider = new DefaultClientArtifactsProvider(new ChaosDiscovery(role)));
        builder.Services.AddSingleton(_ => new ChaosReactor(sideLedger));
        return builder.Build();
    }
}
