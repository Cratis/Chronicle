// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationObserverBindings.given;

public class a_later_generation_binding : Specification
{
    protected IGenerationBuildStorage _storage;
    protected IObserverIdentityClaims _claims;
    protected GenerationObserverBinding _binding;
    protected GenerationObserverBindings _resolver;

    void Establish()
    {
        _storage = Substitute.For<IGenerationBuildStorage>();
        _claims = Substitute.For<IObserverIdentityClaims>();
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"user\"}");
        var key = new MaterializationKey(new(Guid.NewGuid()), "ns", "user", SinkTypeId.None, SinkConfigurationId.None);
        _binding = new(key, MaterializationGenerationIdentity.For(key.Projection, new MaterializationGeneration(2), "models"), EventSequenceId.Log, execution.Revision);
        _storage.ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>()).Returns(_binding);
        _storage.GetExecution("store", key, _binding.Identity.Generation).Returns(execution);
        _resolver = new(_storage, _claims);
    }

    protected Task<GenerationObserverBinding?> Resolve() => _resolver.ResolveObserver("store", "ns", EventSequenceId.Log, _binding.Identity.ObserverId);
}
