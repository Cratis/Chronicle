// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage;

public class when_getting_for_a_scope : given.a_closed_streams_storage
{
    ClosedStream _result;
    ClosedStream _absent;

    async Task Establish()
    {
        await _storage.Close(new(_closure.Scope, "other-owner", 2, null));
        await _storage.Close(new(new(EventSourceId: "other-source"), _closure.Owner, 3, null));
    }

    async Task Because()
    {
        _result = await _storage.GetFor(_closure.Owner, new(EventSourceId: "source-a", EventSourceType: EventSourceType.Unspecified, EventStreamType: new(string.Empty)));
        _absent = await _storage.GetFor("missing-owner", _closure.Scope);
    }

    [Fact] void should_match_only_the_owner_and_normalized_scope() => _result.ShouldEqual(_closure);
    [Fact] void should_return_null_for_a_missing_closure() => _absent.ShouldBeNull();
}
