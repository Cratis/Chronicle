// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

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
    public static IEnumerable<ObserverInformation> ToContract(this IEnumerable<(ObserverDefinition Definition, ObserverState State)> information) =>
        information.Select(_ => _.Definition.ToContract(_.State));

    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="definition"><see cref="ObserverDefinition"/> to convert from.</param>
    /// <param name="state"><see cref="ObserverState"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverInformation"/>.</returns>
    public static ObserverInformation ToContract(this ObserverDefinition definition, ObserverState state) =>
        new()
        {
            Id = definition.Identifier,
            EventSequenceId = definition.EventSequenceId,
            Type = definition.Type.ToContract(),
            Owner = definition.Owner.ToContract(),
            EventTypes = definition.EventTypes.ToContract(),
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            LastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber,
            RunningState = state.RunningState.ToContract()
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
            Concepts.Observation.ObserverType.External => ObserverType.External,
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

    /// <summary>
    /// Convert to contract.
    /// </summary>
    /// <param name="owner"><see cref="Concepts.Observation.ObserverOwner"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverOwner"/>.</returns>
    public static ObserverOwner ToContract(this Concepts.Observation.ObserverOwner owner) =>
        owner switch
        {
            Concepts.Observation.ObserverOwner.Client => ObserverOwner.Client,
            Concepts.Observation.ObserverOwner.Kernel => ObserverOwner.Kernel,
            _ => ObserverOwner.None
        };
}
