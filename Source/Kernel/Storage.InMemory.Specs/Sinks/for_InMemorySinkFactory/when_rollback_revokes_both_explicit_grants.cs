// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.InMemory.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySinkFactory;

public class when_rollback_revokes_both_explicit_grants : Specification
{
    InMemoryMaterializationBindingStorage _storage;
    ISink _active;
    ISink _rollbackTarget;
    MaterializationPublication _result;
    GenerationSinkContext _first;
    GenerationSinkContext _second;
    MaterializationBinding _published;

    async Task Establish()
    {
        _storage = new();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IMaterializationBindingStorage)).Returns(_storage);
        var factory = new InMemorySinkFactory(Substitute.For<ITypeFormats>(), provider);
        _first = GenerationContexts.Create(WellKnownSinkTypes.InMemory);
        await _storage.TryAdoptFirst("store", _first.Binding.Key, new(_first.Binding.Identity, MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch));
        await _storage.TrySetBindingEpoch("store", _first.Binding.Key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _storage.TryAdoptExecution("store", _first.Binding.Key, _first.Execution);
        var firstGrant = (await _storage.TryAcquire("store", _first.Binding, new(Guid.NewGuid()), 0))!;
        var reserved = await _storage.ReserveNext("store", _first.Binding.Key, _first.Execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        var binding = (await _storage.ResolveObserver("store", "namespace", _first.Binding.EventSequence, reserved.Record!.Identity.ObserverId))!;
        var secondGrant = (await _storage.TryAcquire("store", binding, new(Guid.NewGuid()), 0))!;
        _second = new(binding, _first.Execution, secondGrant);
        await _storage.TryMarkFollowing("store", _first.Binding.Key, binding.Identity.Generation);
        await _storage.TryPublish("store", _first.Binding.Key, MaterializationGeneration.First, ServingEpoch.Initial, binding.Identity.Generation, TimeSpan.FromHours(1), new(Guid.NewGuid()));
        _published = (await _storage.Get("store", _first.Binding.Key))!;
        _first = _first with { Grant = (await _storage.TryAcquire("store", _first.Binding, new(Guid.NewGuid()), firstGrant.Epoch + 1))! };
        _second = _second with { Grant = (await _storage.TryAcquire("store", binding, new(Guid.NewGuid()), _published.Fence!.Epoch))! };
        _rollbackTarget = factory.CreateFor("store", "namespace", _first);
        _active = factory.CreateFor("store", "namespace", _second);
    }

    async Task Because() => _result = await _storage.TryRestore("store", _first.Binding.Key, _second.Binding.Identity.Generation, _published.Epoch, MaterializationGeneration.First, TimeSpan.FromHours(1), new(Guid.NewGuid()));

    [Fact] void should_restore() => _result.Result.ShouldEqual(MaterializationPublicationResult.Published);
    [Fact] async Task should_revoke_the_old_active_admission() => (await Catch.Exception(_active.EnsureIndexes)).ShouldBeOfExactType<GenerationWriterGrantRevoked>();
    [Fact] async Task should_revoke_the_rollback_validator_admission() => (await Catch.Exception(_rollbackTarget.EnsureIndexes)).ShouldBeOfExactType<GenerationWriterGrantRevoked>();
    [Fact] async Task should_record_the_exact_target_gate_used_by_the_sink() => (await _storage.Get("store", _first.Binding.Key))!.Fence!.Gate.ShouldEqual(_first.Grant.Gate);
}
