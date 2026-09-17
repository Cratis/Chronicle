// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_persisted_empty_sentinels : given.an_acknowledged_append
{
    AppendResult _result;

    void Establish()
    {
        _response.Receipt.EventSourceType = EventSourceType.Unspecified;
        _response.Receipt.EventStreamId = EventStreamId.NotSet;
        _response.Receipt.Subject = Subject.NotSet;
        _response.Receipt.Hash = EventHash.NotSet;
        _response.Receipt.Causation = [];
        _response.Receipt.Tags = [];
    }

    async Task Because() => _result = await _eventSequence.Append(_source, "event");

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_unspecified_source_type() => _result.Receipt.EventSourceType.ShouldEqual(EventSourceType.Unspecified);
    [Fact] void should_preserve_unset_stream_id() => _result.Receipt.EventStreamId.ShouldEqual(EventStreamId.NotSet);
    // An empty subject on the wire only comes from a server that predates carrying one, where it meant the event
    // source is the subject. A read resolves it the same way, so an append and a read agree on who the event is about.
    [Fact] void should_resolve_an_unset_subject_to_the_event_source() => _result.Receipt.Subject.Value.ShouldEqual(_source.Value);
    [Fact] void should_preserve_unset_hash() => _result.Receipt.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_preserve_empty_causation() => _result.Receipt.Causation.ShouldBeEmpty();
    [Fact] void should_preserve_empty_tags() => _result.Receipt.Tags.ShouldBeEmpty();
}
