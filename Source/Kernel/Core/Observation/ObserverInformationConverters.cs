// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Joins observer definitions with their state and converts them into the observer read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class ObserverInformationConverters
{
    /// <summary>
    /// Joins observer definitions with their state.
    /// </summary>
    /// <param name="definitions">The observer definitions.</param>
    /// <param name="states">The observer states.</param>
    /// <returns>The joined observer information.</returns>
    /// <remarks>
    /// Left outer join on purpose: an observer that has been defined but has not run yet has no state,
    /// and it still belongs in the listing. An inner join would hide it until it first handled an event.
    /// </remarks>
    internal static IEnumerable<ObserverInformation> Join(
        IEnumerable<ObserverDefinition> definitions,
        IEnumerable<ObserverState> states) =>
        from definition in definitions
        join state in states on definition.Identifier equals state.Identifier into stateGroup
        from state in stateGroup.DefaultIfEmpty(ObserverState.Empty)
        select ToObserverInformation(definition, state);

    /// <summary>
    /// Converts an observer definition and its state into the observer read model.
    /// </summary>
    /// <param name="definition">The observer definition.</param>
    /// <param name="state">The observer state.</param>
    /// <returns>The observer information.</returns>
    internal static ObserverInformation ToObserverInformation(ObserverDefinition definition, ObserverState state) =>
        new(
            definition.Identifier,
            definition.EventSequenceId,
            (ObserverType)(int)definition.Type,
            (ObserverOwner)(int)definition.Owner,
            definition.EventTypes.Select(et => et.Id.Value),
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber,
            state.TailEventSequenceNumber,
            state.HandledEventCount,
            (ObserverRunningState)(int)state.RunningState,

            // Subscription state is per-observer and only known by the observer grain, so the listing
            // reports false rather than activating every grain to ask. Read a single observer to get it.
            IsSubscribed: false,
            definition.IsReplayable);
}
