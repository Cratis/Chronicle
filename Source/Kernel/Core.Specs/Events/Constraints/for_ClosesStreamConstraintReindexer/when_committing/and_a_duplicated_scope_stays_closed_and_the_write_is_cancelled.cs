// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintReindexer.when_committing;

public class and_a_duplicated_scope_stays_closed_and_the_write_is_cancelled : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    readonly List<ClosedStream> _legacy = [];
    ClosesStreamConstraintReindexer _reindexer;
    ClosedStreamScope _retained;

    async Task Establish()
    {
        _retained = new ClosedStreamScope(EventSourceId: "source", EventStreamId: "April");
        _legacy.AddRange([
            new(_retained, "closing", 0, null),
            new(_retained with { EventSourceType = EventSourceType.Unspecified }, "closing", 1, null)]);
        _closures.GetForOwner(Arg.Any<ClosedStreamOwner>()).Returns(_ => _legacy.ToArray());

        // SQL-like: reopening a scope deletes every row of the normalized scope at once.
        _closures.Reopen(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var removed = _legacy.RemoveAll(row => row.Owner == call.Arg<ClosedStreamOwner>() && row.Scope.Normalized() == call.Arg<ClosedStreamScope>().Normalized());
            return Task.FromResult(removed > 0);
        });
        _reindexer = new(_closures, NullLogger<ReindexConstraintsStep>.Instance);
        await _reindexer.Initialize([_closing]);
        _reindexer.Include(Event("Closed", 4, Payload("period", "April")), Payload("period", "April"));
    }

    async Task Because() => await Catch.Exception(() => _reindexer.Commit(CancellationToken.None, () => throw new OperationCanceledException()));

    [Fact] void should_never_reopen_the_scope() => _closures.DidNotReceive().Reopen(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>());
    [Fact] void should_leave_the_scope_closed() => _legacy.Count.ShouldEqual(2);
}
