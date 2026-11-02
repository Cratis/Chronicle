// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The sweep uses the database clock: an orphan older than the retention goes, a recent one stays.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_old_receipts_exist<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    const string Orphan = "00000000-0000-0000-0000-0000000000a1";
    const string Recent = "00000000-0000-0000-0000-0000000000a2";
    long _remaining;
    long _recentRemaining;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        await Apply("first", 2, KeyedFromJoin("joinId", "J", ("x", 1)), Set(("group", "g")));
        var now = await DatabaseNow();
        await Execute($"INSERT INTO {Quote(ReceiptTable)} ({Quote("Id")}, {Quote("TableName")}, {Quote("EventSequenceNumber")}, {Quote("CreatedAt")}) VALUES ('{Orphan}', 'x', 1, {now - (20 * 60 * 1000)})");
        await Execute($"INSERT INTO {Quote(ReceiptTable)} ({Quote("Id")}, {Quote("TableName")}, {Quote("EventSequenceNumber")}, {Quote("CreatedAt")}) VALUES ('{Recent}', 'x', 1, {now - (60 * 1000)})");
    }

    async Task Because()
    {
        var fresh = (Sink)AnotherSink();
        fresh.OrphanRetention = TimeSpan.FromMinutes(15);
        await ApplyOn(fresh, "second", 3, KeyedFromJoin("joinId", "J", ("x", 2)), Set(("group", "g")));
        _remaining = await Scalar($"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM {Quote(ReceiptTable)}");
        _recentRemaining = await Scalar($"SELECT CAST(COUNT(*) AS bigint) AS \"Value\" FROM {Quote(ReceiptTable)} WHERE {Quote("EventSequenceNumber")} = 1 AND {Quote("CreatedAt")} > 0 AND {Quote("TableName")} = 'x'");
    }

    [Fact] public void should_delete_the_expired_receipt_and_keep_the_recent_one() => _remaining.ShouldEqual(1);
    [Fact] public void should_keep_the_recent_receipt() => _recentRemaining.ShouldEqual(1);
}
