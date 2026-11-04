// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A retry that is past the verification window and finds no receipt must fail closed: the outcome is unknown, so it is neither verified nor reapplied.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_cycle_retries_after_its_verification_window<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        ((Sink)_sink).ReceiptVerificationWindow = TimeSpan.FromMilliseconds(-1);
        _capture.Clear();
        _capture.FailNextCommitBeforeSending();
    }

    async Task Because()
    {
        try
        {
            await Apply("row", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _row = await Row("row");
    }

    [Fact] public void should_fail_closed() => _error.ShouldBeOfExactType<SinkCycleOutcomeUnknown>();
    [Fact] public void should_not_apply_the_cycle() => _row.ShouldBeNull();
}
