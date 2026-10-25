// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_user_projection_collides_with_a_generation_observer : given.a_projections_manager_grain
{
    ProjectionDefinition _incoming;

    protected override IGenerationProjectionResolver GenerationResolver { get; } = Substitute.For<IGenerationProjectionResolver>();

    void Establish()
    {
        _incoming = CreateDefinition("the-projection@g2", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        GenerationResolver.ResolveCataloged(default!, default!, default!).ReturnsForAnyArgs(Task.FromResult<IReadOnlyCollection<GenerationExecution>>([]));
        GenerationResolver.IsGenerationObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>()).Returns(true);
    }

    async Task Because()
    {
        await _grain.Register([_incoming]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_check_the_observer_identity() => GenerationResolver.Received().IsGenerationObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), _incoming.Identifier);
    [Fact] void should_not_subscribe_the_colliding_observer() => _observerGrain.ReceivedCalls().Any(call => call.GetMethodInfo().Name.StartsWith("Subscribe", StringComparison.Ordinal)).ShouldBeFalse();
}
