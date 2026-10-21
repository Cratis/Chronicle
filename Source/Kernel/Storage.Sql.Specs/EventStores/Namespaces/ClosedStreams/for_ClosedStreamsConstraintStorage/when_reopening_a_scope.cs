// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_reopening_a_scope : given.a_closed_streams_storage
{
    readonly ClosedStreamScope _scope = new(EventSourceId: "source", EventStreamId: "April");
    bool _first;
    bool _second;
    ClosedStream[] _remaining;

    async Task Establish()
    {
        await _storage.Close(new(_scope, "owner", 1, null));
        await _storage.Close(new(_scope, "other-owner", 2, null));
        await _storage.Close(new(new(EventSourceId: "source", EventStreamId: "May"), "owner", 3, null));
    }

    async Task Because()
    {
        _first = await _storage.Reopen("owner", _scope with { EventSourceType = EventSourceType.Unspecified });
        _second = await _storage.Reopen("owner", _scope);
        _remaining = (await _storage.GetForOwner("owner")).Concat(await _storage.GetForOwner("other-owner")).ToArray();
    }

    [Fact] void should_report_the_removal() => _first.ShouldBeTrue();
    [Fact] void should_report_nothing_left_to_remove_on_the_next_call() => _second.ShouldBeFalse();
    [Fact] void should_leave_only_other_owners_and_other_scopes() => _remaining.Length.ShouldEqual(2);
}
