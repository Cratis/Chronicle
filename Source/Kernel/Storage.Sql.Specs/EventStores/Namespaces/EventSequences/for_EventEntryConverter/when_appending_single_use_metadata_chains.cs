// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_appending_single_use_metadata_chains : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    EventEntry _single;
    EventEntry _multi;
    given.a_single_use_sequence<Causation> _causation;
    given.a_single_use_sequence<IdentityId> _identities;
    string _emptyCausation;
    string _emptyIdentities;

    void Because()
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["Name"] = "é😀";
        _causation = Causes();
        _identities = Identities();
        _single = EventEntryConverter.ToEventEntry(42, "sources", "source", "streams", "stream", new("event", 2), new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), _causation, _identities, [new("tag")], _occurred, content, new("hash"), new("subject"));
        _multi = EventEntryConverter.ToEventEntry(42, "sources", "source", "streams", "stream", new("event", 2), new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), Causes(), Identities(), [new("tag")], _occurred, new Dictionary<EventTypeGeneration, ExpandoObject> { [2] = content }, new Dictionary<EventTypeGeneration, EventHash> { [2] = new("hash") }, new("subject"));
        _emptyCausation = EventEntryConverter.SerializeCausation(new given.a_single_use_sequence<Causation>([]));
        _emptyIdentities = EventEntryConverter.SerializeCausedBy(new given.a_single_use_sequence<IdentityId>([]));
    }

    [Fact] void should_pin_causation_bytes() => _single.Causation.ShouldEqual("""[{"occurred":"2026-01-02T03:04:05+00:00","type":{"name":"type","value":"type"},"properties":{"back\\slash":"line\n\uD83D\uDE00\u00E9\u003C\u003E\u0026","quote":"\u0022"}}]""");
    [Fact] void should_pin_identity_bytes_and_order() => _single.CausedBy.ShouldEqual("""["22222222-2222-2222-2222-222222222222","11111111-1111-1111-1111-111111111111"]""");
    [Fact] void should_pin_content_bytes() => _single.Content.ShouldEqual("""{"2":{"Name":"\u00E9\uD83D\uDE00"}}""");
    [Fact] void should_pin_hash_bytes() => _single.ContentHashes.ShouldEqual("""{"2":"hash"}""");
    [Fact] void should_pin_tag_bytes() => _single.Tags.ShouldEqual("""["tag"]""");
    [Fact] void should_preserve_the_sequence_number() => _single.SequenceNumber.ShouldEqual(42UL);
    [Fact] void should_preserve_the_correlation() => _single.CorrelationId.ShouldEqual("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    [Fact] void should_preserve_the_event_type() => _single.Type.Value.ShouldEqual("event");
    [Fact] void should_preserve_the_generation() => _single.AppendedGeneration.ShouldEqual(2u);
    [Fact] void should_preserve_the_timestamp() => _single.Occurred.ShouldEqual(_occurred);
    [Fact] void should_preserve_the_event_source_type() => _single.EventSourceType.Value.ShouldEqual("sources");
    [Fact] void should_preserve_the_event_source_id() => _single.EventSourceId.Value.ShouldEqual("source");
    [Fact] void should_preserve_the_stream_type() => _single.EventStreamType.Value.ShouldEqual("streams");
    [Fact] void should_preserve_the_stream_id() => _single.EventStreamId.Value.ShouldEqual("stream");
    [Fact] void should_preserve_the_subject() => _single.Subject.ShouldEqual("subject");
    [Fact] void should_start_without_compensations() => _single.Compensations.ShouldBeEmpty();
    [Fact] void should_pin_both_entry_overloads() => Assert.Equivalent(_single, _multi);
    [Fact] void should_enumerate_causation_once() => _causation.Enumerations.ShouldEqual(1);
    [Fact] void should_enumerate_identities_once() => _identities.Enumerations.ShouldEqual(1);
    [Fact] void should_pin_empty_causation_bytes() => _emptyCausation.ShouldEqual("[]");
    [Fact] void should_pin_empty_identity_bytes() => _emptyIdentities.ShouldEqual("[]");

    static given.a_single_use_sequence<Causation> Causes() => new([new(_occurred, "type", new Dictionary<string, string> { ["back\\slash"] = "line\n😀é<>&", ["quote"] = "\"" })]);
    static given.a_single_use_sequence<IdentityId> Identities() => new([new(Guid.Parse("22222222-2222-2222-2222-222222222222")), new(Guid.Parse("11111111-1111-1111-1111-111111111111"))]);
}
