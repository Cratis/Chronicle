// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_activated_before_its_definition_is_written;

public class and_a_page_arrives : given.a_subscriber_activated_before_its_definition
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNextPage([_event], new(null));

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_handle_the_event() => _ = _cachedPipeline.Received(1).Handle(_event);
}
