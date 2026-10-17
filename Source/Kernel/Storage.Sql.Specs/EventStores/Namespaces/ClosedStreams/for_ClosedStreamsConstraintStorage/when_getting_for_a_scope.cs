// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_getting_for_a_scope(PostgreSqlFixture fixture) : given.a_postgresql_closed_streams_storage(fixture)
{
    ClosedStream _closure;
    ClosedStream _result;
    ClosedStream _absent;

    async Task Establish()
    {
        _closure = new(new(EventSourceId: "source"), "owner", 5, null);
        await _storage.Close(_closure);
        await _storage.Close(new(_closure.Scope, "other-owner", 2, null));
        await _storage.Close(new(new(EventSourceId: "other-source"), _closure.Owner, 3, null));
        await new ClosedStreamsConstraintStorage(Store, Namespace, EventSequenceId.Outbox, Database).Close(_closure with { SequenceNumber = 8 });
    }

    async Task Because()
    {
        _result = await _storage.GetFor(_closure.Owner, new(EventSourceId: "source", EventSourceType: EventSourceType.Unspecified, EventStreamType: new(string.Empty)));
        _absent = await _storage.GetFor("missing-owner", _closure.Scope);
    }

    [Fact] void should_match_only_the_owner_sequence_and_normalized_scope() => _result.ShouldEqual(_closure);
    [Fact] void should_return_null_for_a_missing_closure() => _absent.ShouldBeNull();
}
