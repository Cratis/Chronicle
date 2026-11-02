// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A cycle with one write keeps today's commands: no explicit transaction, no receipt, no savepoint. The recorded command
/// count and fingerprint were taken from the production code before the change.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_event_has_a_single_write<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    protected abstract int RecordedCommands { get; }

    protected abstract string RecordedFingerprint { get; }

    protected abstract Task Seed();

    protected abstract Task Write();

    async Task Establish()
    {
        await Seed();
        _capture.Clear();
    }

    async Task Because()
    {
        await Write();
        var record = Environment.GetEnvironmentVariable("CHRONICLE_SINK_RECORD");
        if (record is not null)
        {
            await File.AppendAllTextAsync(record, $"{GetType().Name.Replace("_on_sqlite", string.Empty).Replace("_on_postgresql", string.Empty)}|{(IsSqlite ? "sqlite" : "postgresql")}|{_capture.Commands.Count}|{_capture.Fingerprint}\n");
        }
    }

    [Fact] public void should_issue_the_recorded_commands() => _capture.Commands.Count.ShouldEqual(RecordedCommands);
    [Fact] public void should_keep_the_recorded_fingerprint() => _capture.Fingerprint.ShouldEqual(RecordedFingerprint);
    [Fact] public void should_not_start_a_transaction_of_its_own() => _capture.Started.ShouldEqual(0);
    [Fact] public void should_not_touch_the_receipt_table() => ReceiptCommands.ShouldBeEmpty();
    [Fact] public void should_not_create_savepoints() => _capture.Savepoints.ShouldEqual(0);
}
