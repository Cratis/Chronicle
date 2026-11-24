// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents an implementation of <see cref="IObserverDefinitionsStateProvider"/> on top of <see cref="IStorage"/>.
/// </summary>
/// <remarks>
/// Every storage provider wraps its observer definitions in the <see cref="ObserverDefinitionRegistry"/>, so the snapshot is
/// the registry's own and holds the same definition instances every observer activation holds. Plain storage without the
/// registry is read in full every time, which only happens in isolated specifications.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to read through.</param>
[Singleton]
public class ObserverDefinitionsStateProvider(IStorage storage) : IObserverDefinitionsStateProvider
{
    /// <inheritdoc/>
    public async ValueTask<ObserverDefinitionRegistrySnapshot> GetSnapshot(EventStoreName eventStore)
    {
        var observers = storage.GetEventStore(eventStore).Observers;
        if (observers is ObserverDefinitionRegistry registry)
        {
            return await registry.GetSnapshot();
        }

        var definitions = await observers.GetAll();
        return new(0, definitions.ToFrozenDictionary(_ => _.Identifier));
    }

    /// <inheritdoc/>
    public async Task<ObserverDefinitionRevision> GetStoredRevision(EventStoreName eventStore, ObserverId observerId)
    {
        var observers = storage.GetEventStore(eventStore).Observers;
        var stored = observers is ObserverDefinitionRegistry registry
            ? await registry.GetStored(observerId)
            : await observers.Get(observerId);
        return stored.Identifier == observerId ? stored.Revision : ObserverDefinitionRevision.None;
    }

    /// <inheritdoc/>
    public void Changed(EventStoreName eventStore, ObserverId observerId) =>
        storage.GetEventStore(eventStore).Observers.Invalidate(observerId);
}
