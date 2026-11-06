// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_tag_filter : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        var definition = new EventStoreSubscriptionDefinition(
            "subscription",
            "Admin",
            [new EventType("event-type-1", 1), new EventType("event-type-2", 1), new EventType("event-type-3", 1)])
        {
            NamedTags = [new NamedTag(new TagName("audience"), "lobby-a")]
        };
        _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns(_ => [definition]);

        _events =
        [
            _events[0] with { Context = _events[0].Context with { NamedTags = [new NamedTag(new TagName("audience"), "lobby-a")] } },
            _events[1] with { Context = _events[1].Context with { NamedTags = [new NamedTag(new TagName("audience"), "lobby-b")] } },
            _events[2]
        ];

        // Only the named-tag overload is used for tagged events; mock it so the append succeeds.
        _inboxSequence.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>(),
            Arg.Any<IReadOnlyCollection<NamedTag>>(),
            Arg.Any<EventSourceName?>()).Returns(AppendResult.Success(CorrelationId.New(), 1UL));
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_only_forward_the_matching_event() => AppendCalls().ShouldEqual(1);
    [Fact] void should_keep_the_named_tags_on_the_forwarded_event() => ((IReadOnlyCollection<NamedTag>)_inboxSequence.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).GetArguments()[13]!)
        .Single().Value.ShouldEqual("lobby-a");
    [Fact] void should_report_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_move_past_the_filtered_events() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)44UL);
}
