// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_retrying_a_build_operation<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _firstReservation;
    GenerationReservation _retry;
    async Task Establish() => _firstReservation = await Build();
    async Task Because() => _retry = await Build();
    [Fact] public void should_return_the_original_reservation() => _retry.ShouldEqual(_firstReservation);
    [Fact] public async Task should_allocate_only_one_generation() => (await _storage.Get(_store, _key))!.Next.ShouldEqual(new MaterializationGeneration(2));
    [Fact] public async Task should_capture_the_exact_execution() => (await _builds.GetExecution(_store, _key, _retry.Record!.Identity.Generation)).ShouldEqual(_execution);
    [Fact] public async Task should_refuse_a_changed_original_time_offset() => (await Catch.Exception(async () => await _builds.ReserveNext(_store, _key, _execution, _operation, DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromHours(1))))).ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] public async Task should_resolve_the_explicit_observer()
    {
        var record = _retry.Record!;
        var observer = await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, record.Identity.ObserverId);
        observer.ShouldEqual(new GenerationObserverBinding(_key, record.Identity, _execution.EventSequence, _execution.Revision));
    }
}
