// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The unavailable sequence number is ulong.MaxValue: the receipt stores its bits and the cycle neither throws nor overflows.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_sequence_number_is_a_sentinel<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    async Task Establish() => await Apply("target", 1, Set(("joinId", "J")));

    async Task Because()
    {
        try
        {
            await Apply("row", ulong.MaxValue, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_insert_one_receipt() => ReceiptInserts.Count().ShouldEqual(1);
}
