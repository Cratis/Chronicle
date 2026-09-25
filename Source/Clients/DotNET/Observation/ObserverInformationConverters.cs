// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for converting <see cref="Contracts.Observation.ObserverInformation"/> to <see cref="ObserverInformation"/>.
/// </summary>
internal static class ObserverInformationConverters
{
    /// <summary>
    /// Convert to client.
    /// </summary>
    /// <param name="information"><see cref="Contracts.Observation.ObserverInformation"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverInformation"/>.</returns>
    public static ObserverInformation ToClient(this Contracts.Observation.ObserverInformation information) =>
        new(
            information.Id,
            information.EventSequenceId,
            information.Type.ToClient(),
            information.RunningState.ToClient(),
            information.LastHandledEventSequenceNumber,
            information.NextEventSequenceNumber,
            information.HandledEventCount);

    /// <summary>
    /// Convert to client.
    /// </summary>
    /// <param name="type"><see cref="Contracts.Observation.ObserverType"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverType"/>.</returns>
    public static ObserverType ToClient(this Contracts.Observation.ObserverType type) =>
        type switch
        {
            Contracts.Observation.ObserverType.Reactor => ObserverType.Reactor,
            Contracts.Observation.ObserverType.Projection => ObserverType.Projection,
            Contracts.Observation.ObserverType.Reducer => ObserverType.Reducer,
            Contracts.Observation.ObserverType.External => ObserverType.External,
            _ => ObserverType.Unknown
        };
}
