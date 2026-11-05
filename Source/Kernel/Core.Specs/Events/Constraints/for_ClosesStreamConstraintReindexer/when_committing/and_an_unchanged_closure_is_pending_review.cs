// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintReindexer.when_committing;

public class and_an_unchanged_closure_is_pending_review : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    ClosesStreamConstraintReindexer _reindexer;
    ClosedStream _closure;

    async Task Establish()
    {
        var closed = Event("Closed", 5, Payload("period", "April"));
        _closure = new(new(EventSourceId: "source", EventStreamId: "April"), "closing", 5, closed.Context.Occurred, PendingReview: true);
        await _rows.Close(_closure);
        _reindexer = new(_closures, NullLogger<ReindexConstraintsStep>.Instance);
        await _reindexer.Initialize([_closing]);
        _reindexer.Include(closed, closed.Content);
    }

    Task Because() => _reindexer.Commit(CancellationToken.None);

    [Fact] async Task should_not_rewrite_an_unchanged_closure() => await _closures.DidNotReceive().Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_remove_the_closure() => await _closures.DidNotReceive().Reopen(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_preserve_the_pending_review_flag() => (await _rows.GetFor("closing", _closure.Scope))!.PendingReview.ShouldBeTrue();
}
