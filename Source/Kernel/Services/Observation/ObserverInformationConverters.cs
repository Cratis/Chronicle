// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Converters for <see cref="ObserverInformation"/>.
/// </summary>
internal static class ObserverInformationConverters
{
    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="information"><see cref="IEnumerable{ObserverInformation}"/> to convert from.</param>
    /// <returns>Converted <see cref="IEnumerable{ObserverInformation}"/>.</returns>
    public static IEnumerable<ObserverInformation> ToContract(this IEnumerable<Concepts.Observation.ObserverInformation> information) =>
        information.Select(_ => _.ToContract());

    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="information"><see cref="Concepts.Observation.ObserverInformation"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverInformation"/>.</returns>
    public static ObserverInformation ToContract(this Concepts.Observation.ObserverInformation information) =>
        new()
        {
            Id = information.Id,
            EventSequenceId = information.EventSequenceId,
            Type = information.Type.ToContract(),
            EventTypes = information.EventTypes.ToContract(),
            NextEventSequenceNumber = information.NextEventSequenceNumber,
            LastHandledEventSequenceNumber = information.LastHandledEventSequenceNumber,
            RunningState = information.RunningState.ToContract()
        };

    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="type"><see cref="Concepts.Observation.ObserverType"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverType"/>.</returns>
    public static ObserverType ToContract(this Concepts.Observation.ObserverType type) =>
        type switch
        {
            Concepts.Observation.ObserverType.Reactor => ObserverType.Reactor,
            Concepts.Observation.ObserverType.Projection => ObserverType.Projection,
            Concepts.Observation.ObserverType.Reducer => ObserverType.Reducer,
            _ => ObserverType.Unknown
        };

    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="state"><see cref="Concepts.Observation.ObserverRunningState"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverRunningState"/>.</returns>
    public static ObserverRunningState ToContract(this Concepts.Observation.ObserverRunningState state) =>
        state switch
        {
            Concepts.Observation.ObserverRunningState.Active => ObserverRunningState.Active,
            Concepts.Observation.ObserverRunningState.Suspended => ObserverRunningState.Suspended,
            Concepts.Observation.ObserverRunningState.Replaying => ObserverRunningState.Replaying,
            Concepts.Observation.ObserverRunningState.Disconnected => ObserverRunningState.Disconnected,
            _ => ObserverRunningState.Unknown
        };
}
