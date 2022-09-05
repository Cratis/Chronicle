// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Extensions.Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for working with the <see cref="Telemetry"/> configuration.
/// </summary>
public static class TelemetryConfigurationExtensions
{
    /// <summary>
    /// Use telemetry from configuration.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to extend.</param>
    /// <returns>Builder for continuation.</returns>
    public static ISiloBuilder UseTelemetry(this ISiloBuilder builder)
    {
        builder.ConfigureServices(_ =>
        {
            var telemetryConfig = _.GetTelemetryConfig();

            switch (telemetryConfig.Type)
            {
                case TelemetryTypes.AppInsights:
                    var options = telemetryConfig.GetAppInsightsTelemetryOptions();
                    builder.AddApplicationInsightsTelemetryConsumer(options.Key);
                    break;
            }
        });

        return builder;
    }
}
