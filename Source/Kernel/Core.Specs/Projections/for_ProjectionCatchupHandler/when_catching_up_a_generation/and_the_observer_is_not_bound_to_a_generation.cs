// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_the_observer_is_not_bound_to_a_generation : given.a_generation_bound_catchup_handler
{
    Engine.IProjection _projection;
    bool _succeeded;

    void Establish()
    {
        _generations
            .Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>())
            .Returns((GenerationExecution?)null);
        _projection = Substitute.For<Engine.IProjection>();
        _projections.GetCurrent(_details.Key.EventStore, _details.Key.Namespace, _details.Key.ObserverId).Returns(Task.FromResult<Engine.IProjection?>(_projection));
        _legacyPipelines.GetFor(_details.Key.EventStore, _details.Key.Namespace, _projection).Returns(Task.FromResult(_generationPipeline));
    }

    async Task Because() => _succeeded = (await _handler.BeginCatchupFor(_details)).IsSuccess;

    [Fact] void should_succeed() => _succeeded.ShouldBeTrue();
    [Fact] void should_use_the_legacy_pipeline() => _legacyPipelines.Received(1).GetFor(_details.Key.EventStore, _details.Key.Namespace, _projection);
    [Fact] void should_not_admit_a_grant() => _generations.DidNotReceive().Admit(Arg.Any<EventStoreName>(), Arg.Any<GenerationExecution>());
}
