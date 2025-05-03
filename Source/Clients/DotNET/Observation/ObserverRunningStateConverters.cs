// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for converting <see cref="Contracts.Observation.ObserverRunningState"/> to <see cref="ObserverRunningState"/>.
/// </summary>
public static class ObserverRunningStateConverters
{
    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="state"><see cref="Contracts.Observation.ObserverRunningState"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverRunningState"/>.</returns>
    public static ObserverRunningState ToClient(this Contracts.Observation.ObserverRunningState state) =>
        state switch
        {
            Contracts.Observation.ObserverRunningState.Active => ObserverRunningState.Active,
            Contracts.Observation.ObserverRunningState.Suspended => ObserverRunningState.Suspended,
            Contracts.Observation.ObserverRunningState.Replaying => ObserverRunningState.Replaying,
            Contracts.Observation.ObserverRunningState.Disconnected => ObserverRunningState.Disconnected,
            _ => ObserverRunningState.Unknown
        };
}
