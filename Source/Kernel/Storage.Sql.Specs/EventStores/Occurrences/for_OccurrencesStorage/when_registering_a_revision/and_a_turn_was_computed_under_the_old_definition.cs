// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_registering_a_revision;

public class and_a_turn_was_computed_under_the_old_definition : given.an_occurrence_storage
{
    ScheduleCursor _observed;
    ScheduleCursor _registered;
    bool _result;
    IEnumerable<Occurrence> _stored;
    async Task Establish()
    {
        _observed = new(_token, 0, 1, 1, 0);
        await _schedules.TryAddCursor(_observed);
    }
    async Task Because()
    {
        await _schedules.RegisterRevision(_token, 2);
        _registered = (await _schedules.GetCursor(_token))!;
        _result = await RecreateSchedules().TryCommitTurn(new(_observed, _observed with { LastMaterializedDue = 1, Version = 1 }, [_occurrence], [], null));
        _stored = await _storage.GetForTrigger(_trigger);
    }
    [Fact] void should_stamp_the_registered_revision() => _registered.RegisteredRevision.ShouldEqual(2L);
    [Fact] void should_leave_the_directive_pending() => _registered.AppliedRevision.ShouldEqual(1L);
    [Fact] void should_invalidate_the_old_version() => _registered.Version.ShouldEqual(1L);
    [Fact] void should_refuse_the_stale_turn() => _result.ShouldBeFalse();
    [Fact] void should_insert_nothing() => _stored.ShouldBeEmpty();
}
