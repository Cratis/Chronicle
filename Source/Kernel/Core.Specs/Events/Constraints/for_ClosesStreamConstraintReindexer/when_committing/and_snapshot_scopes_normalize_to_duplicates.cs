// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintReindexer.when_committing;

public class and_snapshot_scopes_normalize_to_duplicates : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    readonly List<ClosedStream> _legacy = [];
    ClosesStreamConstraintReindexer _reindexer;
    Exception? _error;

    async Task Establish()
    {
        var retained = new ClosedStreamScope(EventSourceId: "source", EventStreamId: "April");
        var removed = new ClosedStreamScope(EventSourceId: "source", EventStreamId: "May");
        _legacy.AddRange([
            new(retained, "closing", 0, null),
            new(retained with { EventSourceType = EventSourceType.Unspecified }, "closing", 1, null),
            new(removed, "closing", 2, null),
            new(removed with { EventSourceType = EventSourceType.Unspecified }, "closing", 3, null)]);
        _closures.GetForOwner(Arg.Any<ClosedStreamOwner>()).Returns(_ => _legacy.ToArray());
        _closures.Reopen(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var row = _legacy.FirstOrDefault(row => row.Owner == call.Arg<ClosedStreamOwner>() && row.Scope.Normalized() == call.Arg<ClosedStreamScope>().Normalized());
            return Task.FromResult(row is not null && _legacy.Remove(row));
        });
        _closures.Close(Arg.Any<ClosedStream>()).Returns(call =>
        {
            var row = call.Arg<ClosedStream>();
            var index = _legacy.FindIndex(previous => previous.Owner == row.Owner && previous.Scope.Normalized() == row.Scope.Normalized());
            if (index >= 0) _legacy[index] = row;
            else _legacy.Add(row);
            return Task.CompletedTask;
        });
        _reindexer = new(_closures, NullLogger<ReindexConstraintsStep>.Instance);
        await _reindexer.Initialize([_closing]);
        _reindexer.Include(Event("Closed", 4, Payload("period", "April")), Payload("period", "April"));
    }

    async Task Because() => _error = await Catch.Exception(() => _reindexer.Commit(CancellationToken.None));

    [Fact] void should_commit_without_a_duplicate_key_failure() => _error.ShouldBeNull();
    [Fact] void should_keep_exactly_one_rebuilt_scope() => _legacy.Count.ShouldEqual(1);
    [Fact] void should_store_the_rebuilt_position() => _legacy.Single().SequenceNumber.ShouldEqual((EventSequenceNumber)4);
    [Fact] void should_remove_every_stale_duplicate() => _legacy.Exists(row => row.Scope.EventStreamId?.Value == "May").ShouldBeFalse();
}
