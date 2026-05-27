// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for Chronicle meters.
/// </summary>
public static class ChronicleMetersExtensions
{
    /// <summary>
    /// Adds the Chronicle meters to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddChronicleMeters(this IServiceCollection services)
    {
        services.AddNamedMeter(WellKnown.MeterName);
        services.AddNamedActivitySource(WellKnown.MeterName);
        return services;
    }
}
