// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings;

public class when_resolving_explicit_execution : Specification
{
    IGenerationBuildStorage _storage;
    GenerationObserverBinding _binding;
    GenerationObserverBinding _result;
    MaterializationExecutionSpecification _execution;
    void Establish()
    {
        _storage = Substitute.For<IGenerationBuildStorage>();
        _execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"user@g2\"}");
        var key = new MaterializationKey(new(Guid.NewGuid()), "ns", "user@g2", SinkTypeId.None, SinkConfigurationId.None);
        _binding = new(key, MaterializationGenerationIdentity.For(key.Projection, MaterializationGeneration.First, "models"), EventSequenceId.Log, _execution.Revision);
        _storage.ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>()).Returns(_binding);
        _storage.GetExecution("store", key, MaterializationGeneration.First).Returns(_execution);
    }
    async Task Because() => _result = (await new GenerationObserverBindings(_storage).ResolveObserver("store", "ns", EventSequenceId.Log, "user@g2"))!;
    [Fact] void should_resolve_the_explicit_binding() => _result.ShouldEqual(_binding);
    [Fact] void should_preserve_the_suffix_like_user_identity() => _result.Identity.Generation.ShouldEqual(MaterializationGeneration.First);
    [Fact] async Task should_refuse_content_corruption()
    {
        _storage.GetExecution("store", _binding.Key, MaterializationGeneration.First).Returns(_execution with { CanonicalContent = "{\"changed\":true}" });
        (await Catch.Exception(async () => await new GenerationObserverBindings(_storage).ResolveObserver("store", "ns", EventSequenceId.Log, "user@g2"))).ShouldBeOfExactType<InvalidMaterializationExecution>();
    }
}
