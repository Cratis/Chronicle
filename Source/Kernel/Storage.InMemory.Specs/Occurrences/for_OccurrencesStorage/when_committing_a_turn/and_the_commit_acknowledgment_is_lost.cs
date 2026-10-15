// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_committing_a_turn;

public class and_the_commit_acknowledgment_is_lost : given.an_occurrence_storage
{
    Exception _error;
    ScheduleCursor _cursor;
    IEnumerable<Occurrence> _stored;
    async Task Establish() => await _schedules.TryAddCursor(new(_token, 0, 1, 1, 0));
    async Task Because()
    {
        var cursor = (await _schedules.GetCursor(_token))!;
        _error = await Catch.Exception(() => new given.CommitThenFailOccurrencesStorage(_storage, _schedules).TryCommitTurn(new(cursor, cursor with { LastMaterializedDue = 1, Version = 1 }, [_occurrence], [], null)));
        _cursor = (await RecreateSchedules().GetCursor(_token))!;
        _stored = await Recreate().GetForTrigger(_trigger);
    }
    [Fact] void should_surface_acknowledgment_loss() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_keep_the_cursor_with_the_occurrence() => _cursor.LastMaterializedDue.ShouldEqual(1L);
    [Fact] void should_keep_exactly_one_occurrence() => _stored.Count().ShouldEqual(1);
}
