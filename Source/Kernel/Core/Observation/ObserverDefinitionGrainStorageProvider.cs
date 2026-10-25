// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ObserverDefinitionGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverDefinition>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).Observers;
        var stored = await observers.Get(observerKey.ObserverId);

        // Keep a stored event sequence as it is, so an activation under another sequence can tell that the record is
        // not its own. A missing record is on no sequence yet; the key's sequence is the one registering it claims.
        actualGrainState.State = stored with
        {
            Identifier = observerKey.ObserverId,
            EventSequenceId = stored.EventSequenceId == EventSequenceId.Unspecified ? observerKey.EventSequenceId : stored.EventSequenceId
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverDefinition>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        // Definitions are stored by observer identifier alone. Only an activation on the definition's own event
        // sequence may write it, so one addressed on another sequence can never overwrite the real observer's.
        var definition = actualGrainState.State!;
        if (definition.EventSequenceId != observerKey.EventSequenceId)
        {
            throw new ObserverKeyDoesNotMatchDefinition(observerKey, definition.EventSequenceId);
        }

        var observers = storage.GetEventStore(observerKey.EventStore).Observers;
        await observers.Save(definition);
    }
}
