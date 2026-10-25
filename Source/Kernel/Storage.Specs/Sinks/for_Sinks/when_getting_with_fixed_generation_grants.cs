// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_getting_with_fixed_generation_grants : Specification
{
    GenerationSinkContext _first;
    GenerationSinkContext _second;
    ISinkFactory _factory;
    Sinks _sinks;
    ISink _legacy;
    ISink _active;
    ISink _candidate;
    ISink _refreshed;
    ISink _cached;

    void Establish()
    {
        _first = GenerationContexts.Create(WellKnownSinkTypes.InMemory);
        _second = GenerationContexts.Create(WellKnownSinkTypes.InMemory, 2);
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(WellKnownSinkTypes.InMemory);
        _factory.CreateFor("store", "namespace", Arg.Any<ReadModelDefinition>()).Returns(_ => Substitute.For<ISink>());
        _factory.CreateFor("store", "namespace", Arg.Any<GenerationSinkContext>()).Returns(_ => Substitute.For<ISink>());
        _sinks = new("store", "namespace", new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    async Task Because()
    {
        _legacy = await _sinks.GetFor(GenerationContexts.Model(WellKnownSinkTypes.InMemory));
        _active = await _sinks.GetFor(_first);
        _candidate = await _sinks.GetFor(_second);
        _cached = await _sinks.GetFor(_first);
        _refreshed = await _sinks.GetFor(_first with { Grant = _first.Grant with { Epoch = 2 } });
    }

    [Fact] void should_not_share_the_adopted_legacy_mode() => _active.ShouldNotBeSame(_legacy);
    [Fact] void should_not_alias_generations_with_equal_schema_generations() => _candidate.ShouldNotBeSame(_active);
    [Fact] void should_reuse_only_the_exact_admission() => _cached.ShouldBeSame(_active);
    [Fact] void should_create_a_wrapper_for_new_admission() => _refreshed.ShouldNotBeSame(_active);
    [Fact] void should_share_the_exact_gate_publication_revokes() => _first.Grant.Gate.ShouldEqual(new GenerationGateId(_first.Binding.Key, _first.Binding.Identity.Generation));
    [Fact] void should_preserve_the_physical_destination() => _first.GetReadModel("store", "namespace").ContainerName.ShouldEqual(_first.Binding.Identity.Destination);
    [Fact] async Task should_refuse_a_cross_namespace_context_before_the_factory() => (await Catch.Exception(() => _sinks.GetFor(_first with { Binding = _first.Binding with { Key = _first.Binding.Key with { Namespace = "other" } } }))).ShouldBeOfExactType<Concepts.Projections.Generations.InvalidMaterializationExecution>();
}
