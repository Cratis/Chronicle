// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_events_supply_distinct_causation : Sequences.given.an_append_endpoint
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

    async Task Because() => await new AppendManyForEventSources(
        "store",
        "namespace",
        "event-log",
        [
            new("first", "", "", "", new EventType("event", 1, false), "{}", Causation: [new(DateTimeOffset.UnixEpoch, "first", new Dictionary<string, string>())]),
            new("second", "", "", "", new EventType("event", 1, false), "{}", Causation: [new(DateTimeOffset.UnixEpoch, "second", new Dictionary<string, string>())]),
            new("third", "", "", "", new EventType("event", 1, false), "{}")
        ],
        Causation: [new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>())]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_keep_the_batch_causation() => _batchCausation.Single().Type.Name.ShouldEqual("ambient");
    [Fact] void should_pass_the_first_events_causation() => _appendedEvents[0].Causation.Single().Type.Name.ShouldEqual("first");
    [Fact] void should_pass_the_second_events_causation() => _appendedEvents[1].Causation.Single().Type.Name.ShouldEqual("second");
    [Fact] void should_leave_the_third_event_to_inherit_the_batch_causation() => _appendedEvents[2].Causation.ShouldBeNull();
}
