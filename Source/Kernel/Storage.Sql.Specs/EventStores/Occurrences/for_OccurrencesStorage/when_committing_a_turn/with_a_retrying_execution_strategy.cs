// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_committing_a_turn;

public class with_a_retrying_execution_strategy : given.an_occurrence_storage_with_a_retrying_execution_strategy
{
    ScheduleCursor _cursor;
    bool _result;
    Occurrence? _materialized;
    async Task Establish()
    {
        await _schedules.TryAddCursor(new(_token, 0, 1, 1, 0));
        _cursor = (await _schedules.GetCursor(_token))!;
    }
    async Task Because()
    {
        _result = await _schedules.TryCommitTurn(new(_cursor, _cursor with { Version = _cursor.Version + 1, LastMaterializedDue = 1 }, [_occurrence], [], null));
        _materialized = await _storage.Get(_occurrence.Id);
    }
    [Fact] void should_commit_the_turn() => _result.ShouldBeTrue();
    [Fact] void should_materialize_the_occurrence() => _materialized.ShouldNotBeNull();
}
