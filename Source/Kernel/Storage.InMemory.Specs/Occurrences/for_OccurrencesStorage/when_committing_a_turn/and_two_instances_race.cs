// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_committing_a_turn;

public class and_two_instances_race : given.an_occurrence_storage
{
    ScheduleCursor _cursor;
    bool[] _results;
    IEnumerable<Occurrence> _stored;
    IEnumerable<SkippedRange> _ranges;
    async Task Establish()
    {
        _cursor = new(_token, 0, 1, 1, 0);
        await _schedules.TryAddCursor(_cursor);
    }
    async Task Because()
    {
        var second = _occurrence with { Id = new OccurrenceId(Guid.NewGuid().ToString("N")) };
        var firstRange = new SkippedRange(Guid.NewGuid().ToString("N"), _token, 1, 0, 1, 1, "test", null);
        var secondRange = firstRange with { Id = Guid.NewGuid().ToString("N") };
        _results = await StorageRace.Run(() => _schedules.TryCommitTurn(new(_cursor, _cursor with { LastMaterializedDue = 1, Version = 1 }, [_occurrence], [firstRange], null)), () => RecreateSchedules().TryCommitTurn(new(_cursor, _cursor with { LastMaterializedDue = 2, Version = 1 }, [second], [secondRange], null)));
        _stored = await _storage.GetForTrigger(_trigger);
        _ranges = await _schedules.GetSkippedRanges(_token);
    }
    [Fact] void should_commit_only_one_turn() => _results.Count(result => result).ShouldEqual(1);
    [Fact] void should_not_insert_the_losing_turns_occurrence() => _stored.Count().ShouldEqual(1);
    [Fact] void should_not_insert_the_losing_turns_skip() => _ranges.Count().ShouldEqual(1);
}
