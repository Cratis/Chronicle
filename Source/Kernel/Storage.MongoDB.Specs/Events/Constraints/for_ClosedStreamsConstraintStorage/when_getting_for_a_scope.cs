// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ClosedStreamsConstraintStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_for_a_scope(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    ClosedStreamsConstraintStorage _storage;
    ClosedStream _closure;
    ClosedStream _result;
    ClosedStream _absent;

    async Task Establish()
    {
        _storage = new(_database, EventSequenceId.Log);
        _closure = new(new(EventSourceId: "source"), "owner", 5, null);
        await _storage.Close(_closure);
        await _storage.Close(new(_closure.Scope, "other-owner", 2, null));
        await _storage.Close(new(new(EventSourceId: "other-source"), _closure.Owner, 3, null));
    }

    async Task Because()
    {
        _result = await _storage.GetFor(_closure.Owner, new(EventSourceId: "source", EventSourceType: EventSourceType.Unspecified, EventStreamType: new(string.Empty)));
        _absent = await _storage.GetFor("missing-owner", _closure.Scope);
    }

    [Fact] void should_match_only_the_owner_and_normalized_scope() => _result.ShouldEqual(_closure);
    [Fact] void should_return_null_for_a_missing_closure() => _absent.ShouldBeNull();
}
