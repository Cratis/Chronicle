// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Kernel.Orleans.Configuration;

/// <summary>
/// Represents a <see cref="IConfigurationValueResolver"/> for resolving options on <see cref="Telemetry"/>.
/// </summary>
public class TelemetryTypeValueResolver : IConfigurationValueResolver
{
    /// <inheritdoc/>
    public object Resolve(IConfiguration configuration)
    {
        return configuration.GetValue<string>("type") switch
        {
            TelemetryTypes.None => null!,
            TelemetryTypes.AppInsights => new AppInsightsTelemetryOptions(),
            TelemetryTypes.OpenTelemetry => new OpenTelemetryOptions(),
            _ => null!
        };
    }
}
