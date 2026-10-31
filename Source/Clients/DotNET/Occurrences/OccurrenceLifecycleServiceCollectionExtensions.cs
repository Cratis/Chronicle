// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds lifecycle recording to a host that configures Chronicle through its own integration.
/// </summary>
public static class OccurrenceLifecycleServiceCollectionExtensions
{
    /// <summary>
    /// Records startup and handles opt-in per-process shutdown. Generic AddCratisChronicle hosts include this automatically.
    /// ASP.NET Core hosts add this after configuring their Chronicle client.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChronicleOccurrenceLifecycle(this IServiceCollection services)
    {
        services.AddHostedService<OccurrenceLifecycle>();
        return services;
    }
}
