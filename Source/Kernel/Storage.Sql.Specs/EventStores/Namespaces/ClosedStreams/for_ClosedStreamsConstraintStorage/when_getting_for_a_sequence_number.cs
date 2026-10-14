// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_getting_for_a_sequence_number(PostgreSqlFixture fixture) : given.a_postgresql_closed_streams_storage(fixture)
{
    ClosedStream _closure;
    ClosedStream _second;
    IEnumerable<ClosedStream> _result;
    IEnumerable<ClosedStream> _absent;

    async Task Establish()
    {
        _closure = new(new(EventSourceId: "source"), "owner", 5, null);
        _second = new(new(EventSourceId: "second"), _closure.Owner, _closure.SequenceNumber, null);
        await _storage.Close(_closure);
        await _storage.Close(_second);
        await _storage.Close(new(new(EventSourceId: "other-event"), _closure.Owner, 7, null));
        await _storage.Close(new(_closure.Scope, "other-owner", _closure.SequenceNumber, null));
        await new ClosedStreamsConstraintStorage(Store, Namespace, EventSequenceId.Outbox, Database).Close(new(new(EventSourceId: "other-sequence"), _closure.Owner, _closure.SequenceNumber, null));
    }

    async Task Because()
    {
        _result = await _storage.GetForEvent(_closure.Owner, _closure.SequenceNumber);
        _absent = await _storage.GetForEvent(_closure.Owner, 99);
    }

    [Fact] void should_match_only_the_owner_sequence_and_deciding_event() => _result.ShouldContainOnly(_closure, _second);
    [Fact] void should_return_no_closures_for_a_missing_event() => _absent.ShouldBeEmpty();
}
