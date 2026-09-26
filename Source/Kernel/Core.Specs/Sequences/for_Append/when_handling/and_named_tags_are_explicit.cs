// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_Append.when_handling;

public class and_named_tags_are_explicit : Sequences.given.an_append_endpoint
{
    IReadOnlyCollection<Concepts.Events.NamedTag> _namedTags;

    void Establish() => _eventSequence.When(sequence => sequence.Append(
        Arg.Any<EventSourceType>(),
        Arg.Any<EventSourceId>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<Concepts.Events.EventType>(),
        Arg.Any<JsonObject>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Subject?>(),
        Arg.Any<IReadOnlyCollection<Concepts.Events.NamedTag>>()))
        .Do(call => _namedTags = call.ArgAt<IReadOnlyCollection<Concepts.Events.NamedTag>>(13));

    async Task Because() => await new AppendWithNamedTags(
        "store",
        "namespace",
        "event-log",
        "source",
        EventSourceType.Default,
        EventStreamType.All,
        EventStreamId.Default,
        new EventType("event", 1, false),
        "{}",
        [new NamedTag("account", "one"), new NamedTag("account", "one"), new NamedTag("Account", "one"), new NamedTag("account", "")]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_deduplicate_exact_pairs_but_preserve_case_and_empty_values() =>
        _namedTags.Select(tag => (tag.Name.Value, tag.Value)).ShouldEqual([("account", "one"), ("Account", "one"), ("account", "")]);
}
