// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for removing an observer and everything keyed to it.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the caller is working in.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes.</param>
/// <remarks>
/// Delete a read model and its projection, or remove a reactor, and the observer it registered stays behind: it stops
/// being reported by any client, settles into <see cref="ObserverRunningState.Disconnected"/> and sits there forever,
/// with records in the store-level and namespaced observer collections, its projection definition, its handled counts
/// and its failed partitions. Nothing short of editing storage directly got rid of it. This is that operation.
/// <para>
/// Removal covers the whole event store rather than the namespace named here, because an observer's definition is a
/// store-level record: <paramref name="Namespace"/> only says which namespace the caller is working in. It refuses
/// while the observer is running or has a subscribed client in any namespace, so what can be removed is only ever an
/// observer no client is reporting. Sink containers and read model data are left untouched.
/// </para>
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Observers)]
public record RemoveObserver(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by removing the observer and everything keyed to it.
    /// </summary>
    /// <param name="remover">The <see cref="IObserverRemover"/> that performs the removal.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/> describing what happened.</returns>
    /// <remarks>
    /// The whole result travels back, not just the outcome. A refusal that does not say which namespace blocked it
    /// leaves the operator with nowhere to look, and the guard runs across every namespace in the event store.
    /// </remarks>
    public async Task<ObserverRemovalResult> Handle(IObserverRemover remover)
    {
        var eventSequenceId = string.IsNullOrEmpty(EventSequenceId)
            ? Concepts.EventSequences.EventSequenceId.Log
            : (Concepts.EventSequences.EventSequenceId)EventSequenceId;

        return await remover.Remove((EventStoreName)EventStore, (ObserverId)ObserverId, eventSequenceId);
    }
}
