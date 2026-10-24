// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_activated_before_its_definition_is_written;

public class and_the_definition_is_still_missing : given.a_subscriber_activated_before_its_definition
{
    ObserverSubscriberResult _result;

    void Establish() => _storage.GetEventStore(_key.EventStore).Projections.Has(_key.ObserverId).Returns(false);

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_that_the_pipeline_is_not_ready() => _result.ExceptionMessages.ShouldContain("Projection pipeline is not yet ready — will be retried.");
    [Fact] void should_not_build_a_pipeline() => _ = _factory.DidNotReceive().Create(_key.EventStore, _key.Namespace, Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
