// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

public abstract class a_reserved_generation_binding<THarness> : a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    protected IGenerationWriterGrants _grants;
    protected GenerationObserverBinding _binding;

    async Task Establish()
    {
        _grants = (IGenerationWriterGrants)_storage;
        var reservation = await Build();
        _candidate = reservation.Record!.Identity.Generation;
        _binding = (await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, reservation.Record.Identity.ObserverId))!;
    }

    protected Task<GenerationWriterGrant?> Acquire(long expectedEpoch) => _grants.TryAcquire(_store, _binding, new(Guid.NewGuid()), expectedEpoch);
}
