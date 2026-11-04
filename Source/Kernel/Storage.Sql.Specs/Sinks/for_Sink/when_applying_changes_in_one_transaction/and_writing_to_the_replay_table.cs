// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// During a replay the cycle writes to the replay table: a failing cycle must leave the replay table and the primary table as they were.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_writing_to_the_replay_table<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    long _replayX;
    long _primaryRows;
    long _replayRows;

    static ReplayContext Replay() => new(new ReadModelType("cycle-read-model", ReadModelGeneration.First), TableName, $"{TableName}-revert", DateTimeOffset.UtcNow);

    async Task Establish()
    {
        await Apply("old", 1, Set(("group", "primary")));
        await _sink.BeginReplay(Replay());
        await Apply("target", 2, Set(("joinId", "J"), ("x", 1)));
        _capture.Clear();
        _capture.ThrowBeforeCommand(IsReadModelInsert, new InvalidOperationException("injected"));
    }

    async Task Because()
    {
        try
        {
            await Apply("row", 3, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _replayX = await Scalar($"SELECT CAST({Quote("x")} AS bigint) AS \"Value\" FROM {Quote("replay-cycle_rows")} WHERE {Quote("id")} = 'target'");
        _replayRows = await Scalar($"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM {Quote("replay-cycle_rows")}");
        _primaryRows = await Scalar($"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM {Quote(TableName)}");
    }

    [Fact] public void should_fail() => _error.ShouldNotBeNull();
    [Fact] public void should_not_store_the_join_in_the_replay_table() => _replayX.ShouldEqual(1);
    [Fact] public void should_not_add_a_row_to_the_replay_table() => _replayRows.ShouldEqual(1);
    [Fact] public void should_leave_the_primary_untouched() => _primaryRows.ShouldEqual(1);
}
