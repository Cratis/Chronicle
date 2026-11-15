// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Converts observer values to the lowercase enum values the telemetry convention uses for attributes.
/// </summary>
internal static class ObserverTelemetryValues
{
    /// <summary>
    /// Gets the attribute value of an <see cref="ObserverType"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The lowercase value.</returns>
    internal static string For(ObserverType type) => type switch
    {
        ObserverType.Reactor => "reactor",
        ObserverType.Projection => "projection",
        ObserverType.Reducer => "reducer",
        ObserverType.External => "external",
        _ => "unknown"
    };

    /// <summary>
    /// Gets the attribute value of an <see cref="ObserverRunningState"/>.
    /// </summary>
    /// <param name="state">The running state.</param>
    /// <returns>The lowercase value.</returns>
    internal static string For(ObserverRunningState state) => state switch
    {
        ObserverRunningState.Active => "active",
        ObserverRunningState.Suspended => "suspended",
        ObserverRunningState.Replaying => "replaying",
        ObserverRunningState.Disconnected => "disconnected",
        ObserverRunningState.Quarantined => "quarantined",
        ObserverRunningState.ChangingDefinition => "changing_definition",
        ObserverRunningState.Initializing => "initializing",
        _ => "unknown"
    };
}
