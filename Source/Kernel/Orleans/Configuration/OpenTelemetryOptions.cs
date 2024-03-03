// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.Configuration;

/// <summary>
/// Represents the options for the OpenTelemetry telemetry collector.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets the URL to use for connecting to OpenTelemetry.
    /// </summary>
    public string Endpoint { get; init; } = "http://localhost:4317";
}
