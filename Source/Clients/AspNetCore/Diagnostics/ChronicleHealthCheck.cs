// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Diagnostics;

/// <summary>
/// Represents a <see cref="IHealthCheck"/> reporting whether the client holds a live connection to the Chronicle kernel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleHealthCheck"/> class.
/// </remarks>
/// <param name="client"><see cref="IChronicleClient"/> to report on.</param>
/// <param name="options">The <see cref="ChronicleAspNetCoreOptions"/> naming the event store to report on.</param>
public class ChronicleHealthCheck(IChronicleClient client, IOptions<ChronicleAspNetCoreOptions> options) : IHealthCheck
{
    /// <summary>
    /// The name the health check is registered under.
    /// </summary>
    public const string Name = "chronicle";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var eventStore = options.Value.EventStore;

        try
        {
            var store = await client.GetEventStore(eventStore);
            return store.Connection.Lifecycle.IsConnected
                ? HealthCheckResult.Healthy($"Connected to the Chronicle kernel for event store '{eventStore}'.")
                : HealthCheckResult.Unhealthy($"Not connected to the Chronicle kernel for event store '{eventStore}'.");
        }
        catch (Exception ex)
        {
            // Reaching the client at all can fail while the kernel is unreachable, and a health check that
            // propagates that is a health check that reports nothing. An unreachable kernel is precisely the
            // state this exists to report.
            return HealthCheckResult.Unhealthy($"Could not reach the Chronicle kernel for event store '{eventStore}'.", ex);
        }
    }
}
