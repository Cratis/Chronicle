// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings;

public class when_a_bound_observer_has_no_execution : Specification
{
    GenerationObserverBindings _resolver;
    IGenerationBuildStorage _storage;
    Exception _error;
    void Establish()
    {
        _storage = Substitute.For<IGenerationBuildStorage>();
        var key = new MaterializationKey(new StoreIncarnation(Guid.NewGuid()), "ns", "projection@g2", SinkTypeId.None, SinkConfigurationId.None);
        var binding = new GenerationObserverBinding(key, MaterializationGenerationIdentity.For(key.Projection, MaterializationGeneration.First, "models"), EventSequenceId.Log, MaterializationRevision.Unknown);
        _storage.ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>()).Returns(binding);
        _resolver = new(_storage);
    }
    async Task Because() => _error = await Catch.Exception(async () => await _resolver.ResolveObserver("store", "ns", EventSequenceId.Log, "projection@g2"));
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidMaterializationExecution>();
}
