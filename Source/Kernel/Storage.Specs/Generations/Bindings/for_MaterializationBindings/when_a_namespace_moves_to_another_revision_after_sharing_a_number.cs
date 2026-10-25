// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_a_namespace_moves_to_another_revision_after_sharing_a_number<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    readonly MaterializationGeneration _shared = new(2);
    readonly MaterializationGeneration _following = new(3);
    MaterializationKey _firstKey;
    MaterializationKey _secondKey;
    MaterializationExecutionSpecification _otherExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"other\",\"schema\":{\"type\":\"object\"}}");
    MaterializationGenerationRecord _secondRecordBefore;
    GenerationReservation _next;

    async Task Establish()
    {
        _firstKey = await Bind("first");
        _secondKey = await Bind("second");
        await _builds.ReserveNext(_store, _firstKey, _execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        await _builds.ReserveNext(_store, _secondKey, _execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        _secondRecordBefore = SharedRecord(await _storage.Get(_store, _secondKey));
        await _storage.TryAbandon(_store, _firstKey, _shared);
    }

    async Task Because() => _next = await _builds.ReserveNext(_store, _firstKey, _otherExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);

    [Fact] public void should_reserve_the_following_number() => _next.Record!.Identity.Generation.ShouldEqual(_following);
    [Fact] public async Task should_bind_the_following_number_to_the_other_revision() => (await _harness.Claims.Get(_store, _next.Record!.Identity.ObserverId))!.Revision.ShouldEqual(_otherExecution.Revision);
    [Fact] public async Task should_keep_the_shared_claim_on_its_revision() => (await _harness.Claims.Get(_store, MaterializationGenerationIdentity.For(_key.Projection, _shared, "models").ObserverId))!.Revision.ShouldEqual(_execution.Revision);
    [Fact] public async Task should_leave_the_other_namespace_record_unchanged() => SharedRecord(await _storage.Get(_store, _secondKey)).ShouldEqual(_secondRecordBefore);

    MaterializationGenerationRecord SharedRecord(MaterializationBinding? binding) => binding!.Generations.Single(_ => _.Identity.Generation == _shared);

    async Task<MaterializationKey> Bind(string ns)
    {
        var key = _key with { Namespace = ns };
        await _storage.TryAdoptFirst(_store, key, _first);
        await _storage.TrySetBindingEpoch(_store, key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, key, _execution);
        return key;
    }
}
