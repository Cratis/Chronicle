// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintReindexer.when_committing;

public class and_the_store_fails_partway : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    ClosesStreamConstraintReindexer _reindexer;
    Exception? _error;
    ClosedStream _old;
    int _writes;

    async Task Establish()
    {
        _old = new(new(EventSourceId: "untouched"), "closing", EventSequenceNumber.First, null);
        await _rows.Close(_old);
        _reindexer = new(_closures, NullLogger<ReindexConstraintsStep>.Instance);
        await _reindexer.Initialize([_closing]);
        _reindexer.Include(Event("Closed", 1, Payload("period", "April")), Payload("period", "April"));
        _reindexer.Include(Event("Closed", 2, Payload("period", "May")), Payload("period", "May"));
        _closures.Close(Arg.Any<ClosedStream>()).Returns(call => ++_writes == 2 ? Task.FromException(new IOException("Store unavailable")) : _rows.Close(call.Arg<ClosedStream>()));
    }

    async Task Because() => _error = await Catch.Exception(() => _reindexer.Commit(CancellationToken.None));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<IOException>();
    [Fact] async Task should_keep_the_old_scope_until_replacements_are_written() => (await _rows.GetForOwner("closing")).ShouldContain(_old);
    [Fact] async Task should_keep_the_successfully_rebuilt_scope() => (await _rows.GetForOwner("closing")).Any(row => row.Scope.EventStreamId?.Value == "April").ShouldBeTrue();
}
