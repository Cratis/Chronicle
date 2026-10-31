// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage;

public class when_getting_for_a_sequence_number : given.a_closed_streams_storage
{
    ClosedStream _second;
    IEnumerable<ClosedStream> _result;
    IEnumerable<ClosedStream> _absent;

    async Task Establish()
    {
        _second = new(new(EventSourceId: "second"), _closure.Owner, _closure.SequenceNumber, null);
        await _storage.Close(_second);
        await _storage.Close(new(new(EventSourceId: "other-event"), _closure.Owner, 7, null));
        await _storage.Close(new(_closure.Scope, "other-owner", _closure.SequenceNumber, null));
    }

    async Task Because()
    {
        _result = await _storage.GetForEvent(_closure.Owner, _closure.SequenceNumber);
        _absent = await _storage.GetForEvent(_closure.Owner, 99);
    }

    [Fact] void should_match_only_the_owner_and_deciding_event() => _result.ShouldContainOnly(_closure, _second);
    [Fact] void should_return_no_closures_for_a_missing_event() => _absent.ShouldBeEmpty();
}
