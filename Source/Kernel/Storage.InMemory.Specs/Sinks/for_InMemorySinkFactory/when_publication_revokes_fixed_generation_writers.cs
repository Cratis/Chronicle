// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.InMemory.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySinkFactory;

public class when_publication_revokes_fixed_generation_writers : Specification
{
    InMemoryMaterializationBindingStorage _bindings;
    InMemorySinkFactory _factory;
    GenerationSinkContext _first;
    GenerationSinkContext _second;
    ISink _active;
    ISink _candidate;
    ISink _fresh;
    IServiceProvider _services;
    MaterializationPublication _publication;
    readonly Key _key = new("one", ArrayIndexers.NoIndexers);

    async Task Establish()
    {
        _bindings = new();
        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(IMaterializationBindingStorage)).Returns(_bindings);
        _factory = new(Substitute.For<ITypeFormats>(), _services);
        _first = GenerationContexts.Create(WellKnownSinkTypes.InMemory);
        await _bindings.TryAdoptFirst("store", _first.Binding.Key, new(_first.Binding.Identity, MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch));
        await _bindings.TrySetBindingEpoch("store", _first.Binding.Key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _bindings.TryAdoptExecution("store", _first.Binding.Key, _first.Execution);
        _first = _first with { Grant = (await _bindings.TryAcquire("store", _first.Binding, new(Guid.NewGuid()), 0))! };
        var reserved = await _bindings.ReserveNext("store", _first.Binding.Key, _first.Execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        var secondBinding = (await _bindings.ResolveObserver("store", "namespace", _first.Binding.EventSequence, reserved.Record!.Identity.ObserverId))!;
        _second = new(secondBinding, _first.Execution, (await _bindings.TryAcquire("store", secondBinding, new(Guid.NewGuid()), 0))!);
        _active = _factory.CreateFor("store", "namespace", _first);
        _candidate = _factory.CreateFor("store", "namespace", _second);
        dynamic activeState = new ExpandoObject();
        activeState.name = "active";
        dynamic candidateState = new ExpandoObject();
        candidateState.name = "candidate";
        await _active.ReplaceIsolated(_key, (ExpandoObject)activeState, 1UL);
        await _candidate.ReplaceIsolated(_key, (ExpandoObject)candidateState, 1UL);
        await _candidate.BeginBulk();
        await _bindings.TryMarkFollowing("store", _first.Binding.Key, _second.Binding.Identity.Generation);
    }

    async Task Because()
    {
        _publication = await _bindings.TryPublish("store", _first.Binding.Key, MaterializationGeneration.First, ServingEpoch.Initial, _second.Binding.Identity.Generation, TimeSpan.FromHours(1), new PublicationReceiptId(Guid.NewGuid()));
        var published = (await _bindings.Get("store", _first.Binding.Key))!;
        var grant = (await _bindings.TryAcquire("store", _second.Binding, new(Guid.NewGuid()), published.Fence!.Epoch))!;
        _fresh = new InMemorySinkFactory(Substitute.For<ITypeFormats>(), _services).CreateFor("store", "namespace", _second with { Grant = grant });
    }

    [Fact] void should_publish() => _publication.Result.ShouldEqual(MaterializationPublicationResult.Published);
    [Fact] async Task should_refuse_the_old_active_writer() => (await Catch.Exception(() => _active.ReplaceIsolated(_key, null, 2UL))).ShouldBeOfExactType<GenerationWriterGrantRevoked>();
    [Fact] async Task should_refuse_the_old_candidate_flush() => (await Catch.Exception(_candidate.FlushBulk)).ShouldBeOfExactType<GenerationWriterGrantRevoked>();
    [Fact] void should_not_reacquire_a_current_epoch_when_recreating_a_stale_sink() => Catch.Exception(() => _factory.CreateFor("store", "namespace", _second)).ShouldBeOfExactType<GenerationWriterGrantRevoked>();
    [Fact] async Task should_keep_candidate_data_across_owner_and_factory_refresh() => ((IDictionary<string, object?>)(await _fresh.FindOrDefault(_key))!)["name"].ShouldEqual("candidate");
    [Fact] async Task should_keep_the_old_active_destination_isolated() => ((IDictionary<string, object?>)(await _active.FindOrDefault(_key))!)["name"].ShouldEqual("active");
    [Fact] async Task should_refuse_a_delayed_original_owner_transition() => (await _bindings.TryAcquire("store", _second.Binding, new(Guid.NewGuid()), 0)).ShouldBeNull();
    [Fact] async Task should_refuse_legacy_destructive_replay_on_a_generation_writer() => (await Catch.Exception(() => _fresh.PrepareInitialRun())).ShouldBeOfExactType<MaterializationBindingsNotSupported>();
}
