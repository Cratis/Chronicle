// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_capturing_adopted_execution<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationExecutionSpecification _snapshot;
    async Task Because() => _snapshot = (await _builds.GetExecution(_store, _key, MaterializationGeneration.First))!;
    [Fact] public void should_preserve_unknown_historical_provenance() => _snapshot.HasKnownHistoricalProvenance.ShouldBeFalse();
    [Fact] public void should_capture_current_execution() => _snapshot.CanonicalContent.ShouldEqual(_execution.CanonicalContent);
    [Fact] public async Task should_leave_the_adopted_revision_unknown() => (await _storage.Get(_store, _key))!.Generations.Single().Revision.ShouldEqual(MaterializationRevision.Unknown);
    [Fact] public async Task should_keep_the_legacy_observer_id() => (await _builds.ResolveObserver(_store, _key.Namespace, _execution.EventSequence, _key.Projection))!.Identity.Generation.ShouldEqual(MaterializationGeneration.First);
}
