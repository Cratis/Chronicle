// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the telemetry configuration.
/// </summary>
[Configuration]
public class Telemetry
{
    /// <summary>
    /// Gets the type of telemetry collection to use.
    /// </summary>
    public string Type { get; init; } = TelemetryTypes.None;

    /// <summary>
    /// Gets the options in the form of a JSON representation for the cluster configuration.
    /// </summary>
    [ConfigurationValueResolver(typeof(TelemetryTypeValueResolver))]
    public object Options { get; init; } = null!;
}
