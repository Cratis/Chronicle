// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.when_a_poll_fails;

public class and_the_snapshot_has_not_changed : given.a_live_query
{
    void Establish()
    {
        _script.Enqueue(() => [1]);
        _script.Enqueue(() => throw new SqliteException("database is locked", 5));
        _script.Enqueue(() => [1]);
        _script.Enqueue(() => [2]);
    }

    async Task Because()
    {
        Subscribe();
        for (var poll = 1; poll <= 4; poll++)
        {
            await _time.WaitForTimers(poll);
            if (poll < 4)
            {
                _time.Advance(Interval);
            }
        }
    }

    [Fact] void should_not_emit_the_unchanged_snapshot_again() => _emissions.Count(snapshot => snapshot.SequenceEqual([1])).ShouldEqual(1);
    [Fact] void should_eventually_emit_the_changed_snapshot() => _emissions[^1].ShouldEqual([2]);
    [Fact] void should_not_report_an_error() => _error.ShouldBeNull();
}
