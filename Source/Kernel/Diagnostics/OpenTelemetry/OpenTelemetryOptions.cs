// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Represents the options for configuring Open Telemetry.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets the open telemetry endpoint.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the open telemetry service name.
    /// </summary>
    public string ServiceName { get; set; } = "Cratis";

    /// <summary>
    /// Gets or sets a value indicating whether open telemetry logging is enabled. Defaults to true.
    /// </summary>
    public bool Logging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether open telemetry tracing is enabled. Defaults to true.
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether open telemetry metrics is enabled. Defaults to true.
    /// </summary>
    public bool Metrics { get; set; } = true;
}
