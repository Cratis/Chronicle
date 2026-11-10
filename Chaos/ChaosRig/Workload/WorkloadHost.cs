// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Chaos;

internal static class WorkloadHost
{
    public static IHost Create(string role, string connection, string store, SideLedger sideLedger)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.AddCratisChronicle(options =>
        {
            options.ConnectionString = new(connection);
            options.EventStore = store;
            options.ActivationScope = ActivationScope.PerProcess;
            options.AutoDiscoverAndRegister = role != "reader";
        }, configure: chronicle => chronicle.ClientArtifactsProvider = new DefaultClientArtifactsProvider(new ChaosDiscovery(role)));
        builder.Services.AddSingleton(_ => new ChaosReactor(sideLedger));
        return builder.Build();
    }
}
