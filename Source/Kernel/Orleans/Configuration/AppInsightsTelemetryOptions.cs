// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the options for the Azure Application Insights telemetry collector.
/// </summary>
public class AppInsightsTelemetryOptions
{
    /// <summary>
    /// Gets the instrumentation key to use for connecting to Application Insights.
    /// </summary>
    public string Key { get; init; } = string.Empty;
}
