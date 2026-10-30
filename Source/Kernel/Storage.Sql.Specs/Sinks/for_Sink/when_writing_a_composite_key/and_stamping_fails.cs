// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_stamping_fails : given.a_composite_key_sink
{
    readonly FailingStamp _failure = new();
    string _name;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness { Interceptors = [_failure] };

    async Task Because()
    {
        await Write("a", "b", "committed");
        await using var context = CreateContext();
        _name = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
    }

    [Fact] void should_attempt_the_stamp() => _failure.Attempts.ShouldEqual(1);
    [Fact] void should_stamp_only_after_the_write_is_committed() => _failure.CommittedName.ShouldEqual("committed");
    [Fact] void should_stamp_outside_the_write_transaction() => _failure.HasTransaction.ShouldBeFalse();
    [Fact] void should_never_fail_the_committed_write() => _name.ShouldEqual("committed");

    sealed class FailingStamp : DbCommandInterceptor
    {
        internal int Attempts { get; private set; }
        internal string? CommittedName { get; private set; }
        internal bool HasTransaction { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) && command.CommandText.Contains("__compositeKey", StringComparison.Ordinal))
            {
                Attempts++;
                HasTransaction = command.Transaction is not null;
                await using var connection = new SqliteConnection(command.Connection!.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                await using var read = connection.CreateCommand();
                read.CommandText = "SELECT name FROM composite_shadows WHERE id='a_b'";
                CommittedName = (string?)await read.ExecuteScalarAsync(cancellationToken);
                throw new InvalidOperationException("Simulated stamp failure after commit");
            }
            return result;
        }
    }
}
