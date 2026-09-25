// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

/// <summary>
/// The line that keeps skipping honest. An event the definition does not name is skipped, but one it *does* name
/// and the pipeline still cannot handle means the pipeline is behind the definition, not that the event is
/// irrelevant. Skipping that one would silently drop an event the projection is supposed to observe, so it fails
/// the partition instead and gets retried once the pipeline catches up.
/// </summary>
public class when_the_pipeline_cannot_handle_an_event_the_definition_names : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        var eventType = new EventType("the-named-event", 1);
        _event = AppendedEvent.EmptyWithEventType(eventType);
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _projection.Accepts(eventType).Returns(true);
        _cachedPipeline.Handle(_event).Returns(Task.FromException<ProjectionEventContext>(new MissingKeyResolverForEventType(eventType)));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_have_handed_the_event_to_the_pipeline() => _ = _cachedPipeline.Received(1).Handle(_event);
    [Fact] void should_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_the_cause() => _result.ExceptionMessages.ShouldContain("Missing key resolver for 'the-named-event+1'");
}
