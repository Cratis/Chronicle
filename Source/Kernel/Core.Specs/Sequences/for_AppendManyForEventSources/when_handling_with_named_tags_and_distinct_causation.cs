// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources;

public class when_handling_with_named_tags_and_distinct_causation : Sequences.given.an_append_endpoint
{
    Concepts.Auditing.Causation[] _batchCausation;

    void Establish()
    {
        _eventSequence.When(_ => _.AppendMany(
            Arg.Any<IEnumerable<EventSequences.EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>()))
            .Do(call => _batchCausation = call.ArgAt<IEnumerable<Concepts.Auditing.Causation>>(2).ToArray());
    }

    async Task Because() => await new AppendManyForEventSourcesWithNamedTags(
        "store",
        "namespace",
        "event-log",
        [
            new("first", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "one")], Causation: [new(DateTimeOffset.UnixEpoch, "first", new Dictionary<string, string>())]),
            new("second", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "two")], Causation: [new(DateTimeOffset.UnixEpoch, "second", new Dictionary<string, string>())]),
            new("third", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "three")])
        ],
        Causation: [new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>())]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_keep_batch_causation() => _batchCausation.Single().Type.Name.ShouldEqual("ambient");
    [Fact] void should_keep_first_events_causation_and_named_tag() => (_appendedEvents[0].Causation.Single().Type.Name, _appendedEvents[0].NamedTags.Single().Value).ShouldEqual(("first", "one"));
    [Fact] void should_keep_second_events_causation_and_named_tag() => (_appendedEvents[1].Causation.Single().Type.Name, _appendedEvents[1].NamedTags.Single().Value).ShouldEqual(("second", "two"));
    [Fact] void should_inherit_batch_causation_only_for_third_event() => _appendedEvents[2].Causation.ShouldBeNull();
}
