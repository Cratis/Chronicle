// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Diagnostics.Identifiers;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_TagExtensions;

public class when_adding_compatibility_keys : Specification
{
    readonly Activity _activity = new("compatibility");
    readonly Activity _hashed = new("hashed");
    readonly Activity _omitted = new("omitted");

    void Because()
    {
        _activity.Tag((EventStoreName)"store").Tag((EventStoreNamespaceName)"namespace").Tag((EventSequenceId)"log")
            .Tag((EventSourceType)"customer").Tag((EventSourceId)"customer-42", new IdentifierTelemetry(new()))
            .Tag(new EventType("type", EventTypeGeneration.First));
        _hashed.Tag((EventSourceId)"customer-42", new IdentifierTelemetry(new()
        {
            EventSourceIdTracing = EventSourceIdTracing.Hashed,
            EventSourceIdHashKey = "chronicle-identifier-hash-key-for-specs-0001"
        }));
        _omitted.Tag((EventSourceId)"customer-42", new IdentifierTelemetry(new() { EventSourceIdTracing = EventSourceIdTracing.Omitted }));
    }

    void Destroy()
    {
        _activity.Dispose();
        _hashed.Dispose();
        _omitted.Dispose();
    }

    [Fact] void should_keep_the_legacy_store_key() => _activity.GetTagItem("cratis.eventstore.name").ShouldEqual("store");
    [Fact] void should_add_the_shared_store_key() => _activity.GetTagItem("cratis.event_store.name").ShouldEqual("store");
    [Fact] void should_keep_both_namespace_keys() => _activity.GetTagItem("cratis.event_store.namespace").ShouldEqual(_activity.GetTagItem("cratis.eventstore.namespace"));
    [Fact] void should_keep_both_sequence_keys() => _activity.GetTagItem("cratis.event_sequence.id").ShouldEqual(_activity.GetTagItem("cratis.eventsequence.id"));
    [Fact] void should_keep_both_source_type_keys() => _activity.GetTagItem("cratis.event_source.type").ShouldEqual(_activity.GetTagItem("cratis.eventsource.type"));
    [Fact] void should_keep_both_event_type_keys() => _activity.GetTagItem("cratis.event_type.id").ShouldEqual(_activity.GetTagItem("cratis.eventtype.id"));
    [Fact] void should_keep_the_raw_source_id() => _activity.GetTagItem("cratis.event_source.id").ShouldEqual("customer-42");
    [Fact] void should_hash_both_copies_identically() => _hashed.GetTagItem("cratis.event_source.id").ShouldEqual(_hashed.GetTagItem("cratis.eventsource.id"));
    [Fact] void should_not_disclose_the_raw_id_under_either_key() => _hashed.TagObjects.Any(_ => Equals(_.Value, "customer-42")).ShouldBeFalse();
    [Fact] void should_omit_the_legacy_source_id() => _omitted.GetTagItem("cratis.eventsource.id").ShouldBeNull();
    [Fact] void should_omit_the_shared_source_id() => _omitted.GetTagItem("cratis.event_source.id").ShouldBeNull();
}
