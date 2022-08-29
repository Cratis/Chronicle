// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Extension methods for working with the <see cref="Telemetry"/> config.
/// </summary>
public static class TelemetryConfigurationExtensions
{
    /// <summary>
    /// Get configured telemetry config from the configured services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to get from.</param>
    /// <returns><see cref="Telemetry"/> config.</returns>
    /// <remarks>
    /// If the config is not configured, a default setup for localhost will be returned.
    /// </remarks>
    public static Telemetry GetTelemetryConfig(this IServiceCollection services) => services.FirstOrDefault(service => service.ServiceType == typeof(Telemetry))?.ImplementationInstance as Telemetry ?? new Telemetry();

    /// <summary>
    /// Get specific <see cref="AppInsightsTelemetryOptions"/> from the options of <see cref="Telemetry"/>.
    /// </summary>
    /// <param name="telemetryConfig"><see cref="Telemetry"/> config to get from.</param>
    /// <returns><see cref="AppInsightsTelemetryOptions"/> instance.</returns>
    public static AppInsightsTelemetryOptions GetAppInsightsTelemetryOptions(this Telemetry telemetryConfig) => telemetryConfig.Options as AppInsightsTelemetryOptions ?? new AppInsightsTelemetryOptions();
}
