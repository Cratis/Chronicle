// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The receipt key is taken when the cycle inserts its receipt, which can only be an earlier attempt of the same call that committed: no rerun and no exception.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_original_commit_lands_after_verification<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    protected virtual bool OnTheCyclesConnection => false;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        _capture.Clear();
        _capture.BeforeCommand(command => command.Contains(ReceiptTable, StringComparison.Ordinal) && command.Contains("INSERT", StringComparison.OrdinalIgnoreCase), async command =>
        {
            var id = command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).OfType<Guid>().First();
            var sql = $"INSERT INTO {Quote(ReceiptTable)} ({Quote("Id")}, {Quote("TableName")}, {Quote("EventSequenceNumber")}, {Quote("CreatedAt")}) VALUES ('{id}', 'x', 1, 1)";
            if (OnTheCyclesConnection)
            {
                await ExecuteOn(command, sql);
            }
            else
            {
                await Execute(sql);
            }
        });
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
    }

    [Fact] public void should_not_throw() => _error.ShouldBeNull();
    [Fact] public void should_not_rerun() => _capture.Started.ShouldEqual(1);
}
