// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// A single write never touches the receipts, so Verify has nothing to validate even when they are absent.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_cycle_has_one_write<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _error;

    async Task Establish()
    {
        await Seed();
        _verify = true;
        _capture.Clear();
    }

    async Task Because()
    {
        try
        {
            await ApplyOn(AnotherSink(), "single", 2, Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_not_touch_the_receipts() => ReceiptCommands.ShouldBeEmpty();
    [Fact] public void should_not_read_the_catalog() => CatalogReads.ShouldBeEmpty();
    [Fact] public void should_not_start_a_transaction() => _capture.Started.ShouldEqual(0);
}
