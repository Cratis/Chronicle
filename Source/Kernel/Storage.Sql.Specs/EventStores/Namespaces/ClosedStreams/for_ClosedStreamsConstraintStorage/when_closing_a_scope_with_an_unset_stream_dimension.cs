// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_closing_a_scope_with_an_unset_stream_dimension : given.a_closed_streams_storage
{
    readonly ClosedStreamScope _scope = new(EventSourceId: "source");
    IEnumerable<ClosedStream> _covering;
    int _count;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.ClosedStreams.Add(new ClosedStreamEntry
        {
            EventSequenceId = EventSequenceId.Log,
            EventSourceId = "source",
            Dimensions = (int)(ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType),
            SequenceNumber = EventSequenceNumber.First.Value
        });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await _storage.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        await using var context = CreateContext();
        _count = await context.ClosedStreams.CountAsync();
        _covering = await _storage.GetCovering(_scope, [ClosedStreamDimensions.EventSourceId]);
    }

    [Fact] void should_not_alias_a_stored_empty_stream_value_with_an_unset_dimension() => _count.ShouldEqual(2);
    [Fact] void should_match_only_the_requested_mask() => _covering.Count().ShouldEqual(1);
}
