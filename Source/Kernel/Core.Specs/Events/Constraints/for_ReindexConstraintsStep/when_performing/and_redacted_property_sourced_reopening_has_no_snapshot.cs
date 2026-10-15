// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_redacted_property_sourced_reopening_has_no_snapshot : given.an_unrecoverable_property_sourced_transition
{
    protected override EventTypeId RedactedType => "Reopened";

    async Task Because() => await Perform();

    [Fact] void should_not_fail_the_job() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_leave_all_existing_owner_rows_untouched() => (await _rows.GetForOwner("closing")).ShouldContainOnly(_existing);
    [Fact] async Task should_never_clear_the_skipped_owner() => await _closures.DidNotReceive().RemoveAllFor(new ClosedStreamOwner("closing"));
    [Fact] async Task should_never_write_a_replayed_row_for_the_skipped_owner() => await _closures.DidNotReceive().Close(Arg.Is<ClosedStream>(row => row.Owner.Value == "closing"));
    [Fact] async Task should_rebuild_the_other_constraint() => (await _rows.GetForOwner("other")).Single().Scope.ShouldEqual(new ClosedStreamScope(EventSourceId: "source"));
}
