// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_committing_a_turn;

public class and_the_directive_is_pending : given.an_occurrence_storage
{
    ScheduleCursor _registered;
    ScheduleCursor _stored;
    bool _result;
    async Task Establish()
    {
        await _schedules.TryAddCursor(new(_token, 0, 1, 1, 0));
        await _schedules.RegisterRevision(_token, 2);
        _registered = (await _schedules.GetCursor(_token))!;
    }
    async Task Because()
    {
        _result = await _schedules.TryCommitTurn(new(_registered, _registered with { AppliedRevision = 2, Version = 2, LastMaterializedDue = 1 }, [_occurrence], [], null));
        _stored = (await _schedules.GetCursor(_token))!;
    }
    [Fact] void should_commit_the_directive() => _result.ShouldBeTrue();
    [Fact] void should_mark_the_revision_applied() => _stored.AppliedRevision.ShouldEqual(2L);
    [Fact] void should_advance_the_cursor() => _stored.LastMaterializedDue.ShouldEqual(1L);
}
