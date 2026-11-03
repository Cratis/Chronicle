// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder;

public class when_handling_events_without_begin : given.a_rebuilder
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _rebuilder.OnNext(_partition, [EventAt(1)], new(null));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_say_the_rebuild_has_not_begun() => _result.ExceptionMessages.ShouldContain(ProjectionPartitionRebuilder.NotBegun);
    [Fact] void should_not_handle_anything() => _pipeline.DidNotReceive().Handle(Arg.Any<Concepts.Events.AppendedEvent>());
}
