// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="chronicleClient">The <see cref="IChronicleClient"/> to work with.</param>
/// <param name="options">The <see cref="ChronicleAspNetCoreOptions"/> to get configuration from.</param>
public class ChronicleClientStartupTask(IChronicleClient chronicleClient, IOptions<ChronicleAspNetCoreOptions> options) : IHostedService
{
    /// <summary>
    /// Internal property for enabling or disabling registration.
    /// </summary>
    internal static bool RegistrationEnabled = true;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!RegistrationEnabled)
        {
            return;
        }

        var eventStore = await chronicleClient.GetEventStore(options.Value.EventStore);
        await eventStore.DiscoverAll();
        await eventStore.RegisterAll();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
