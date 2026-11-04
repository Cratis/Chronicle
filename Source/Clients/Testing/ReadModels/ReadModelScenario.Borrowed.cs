// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

public partial class ReadModelScenario<TReadModel>
{
    readonly EventStoreForTesting? _borrowedStore;
    readonly EventSequenceId? _syntheticInputSequenceId;
    ReadModelsForTesting? _scopedReadModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class that borrows a store.
    /// </summary>
    /// <param name="borrowedStore">The store whose artifacts, event types and serialization the scenario folds through.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reducer and projection instances.</param>
    /// <param name="syntheticInputSequenceId">The sequence holding this scenario's input folded without original contexts.</param>
    ReadModelScenario(EventStoreForTesting borrowedStore, IServiceProvider? serviceProvider, EventSequenceId syntheticInputSequenceId)
        : this(null, Defaults.For(borrowedStore), serviceProvider)
    {
        _borrowedStore = borrowedStore;
        _syntheticInputSequenceId = syntheticInputSequenceId;
    }

    /// <summary>
    /// Creates a scenario that folds through a borrowed store while owning its input, seeds, instances and read model surface.
    /// </summary>
    /// <param name="borrowedStore">The store to borrow.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reducer and projection instances.</param>
    /// <returns>The borrowing scenario.</returns>
    internal static ReadModelScenario<TReadModel> Borrowing(EventStoreForTesting borrowedStore, IServiceProvider? serviceProvider) =>
        new(borrowedStore, serviceProvider, new EventSequenceId($"testing-read-model-input-{Guid.NewGuid()}"));

    ReadModelsForTesting SeededReadModels() => _borrowedStore is null
        ? (ReadModelsForTesting)EventStore().ReadModels
        : _scopedReadModels ??= AttachSeeds(_borrowedStore.CreateScopedReadModels());

    void RegisterSeed(EventSourceId eventSourceId, TReadModel readModel)
    {
        if (_borrowedStore is null)
        {
            _eventStore?.RegisterReadModelInstance(eventSourceId, readModel);
            return;
        }

        _scopedReadModels?.RegisterInstance(eventSourceId, readModel);
    }

    ReadModelsForTesting AttachSeeds(ReadModelsForTesting readModels)
    {
        readModels.BeforeInstanceRead = () =>
        {
            if (_readModelSeeds.Count > 0 || _namedReadModelSeeds.Count > 0) EnsureProcessed();
        };
        foreach (var (key, seed) in _readModelSeeds)
        {
            readModels.RegisterInstance(key, seed);
        }

        return readModels;
    }
}
