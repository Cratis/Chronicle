// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_activated_before_its_definition_is_written;

public class and_the_definition_is_written_without_notification : given.a_subscriber_activated_before_its_definition
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_build_the_pipeline_from_the_stored_definition() => _ = _factory.Received(1).Create(_key.EventStore, _key.Namespace, _definition, Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_handle_the_event() => _ = _cachedPipeline.Received(1).Handle(_event);
    [Fact] void should_not_call_back_into_the_notifying_grain() => _ = _projectionGrain.DidNotReceive().GetDefinition();
}
