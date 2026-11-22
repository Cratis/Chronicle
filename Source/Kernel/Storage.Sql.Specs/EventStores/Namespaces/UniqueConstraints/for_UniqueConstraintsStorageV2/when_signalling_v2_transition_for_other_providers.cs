// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;
using Cratis.Monads;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public class when_signalling_v2_transition_for_other_providers
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_ignore_the_select_and_signal_at_transaction_start(bool postgres)
    {
        var interleaving = new V2Interleaving();
        var signalledAtSelect = true;
        var signalledAtStart = false;
        await interleaving.Run(
            async () =>
            {
                await using DbConnection connection = postgres ? new NpgsqlConnection() : new SqliteConnection();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM UniqueConstraintAuthoritiesV2";
                await interleaving.Commands.ReaderExecutingAsync(command, null!, default, default);
                signalledAtSelect = interleaving.TransitionWasAttempted;
                await interleaving.TransactionStartingAsync(connection, null!, default(InterceptionResult<DbTransaction>), default);
                signalledAtStart = interleaving.TransitionWasAttempted;
            },
            async () =>
            {
                await using DbConnection connection = postgres ? new NpgsqlConnection() : new SqliteConnection();
                await using var command = connection.CreateCommand();
                await interleaving.TransactionStartingAsync(connection, null!, default(InterceptionResult<DbTransaction>), default);
                command.CommandText = "UPDATE UniqueConstraintAuthoritiesV2 SET Revision = Revision";
                await interleaving.Commands.NonQueryExecutedAsync(command, null!, 0, default);
                return Result<bool, UniqueConstraintWriteFailure>.Success(true);
            },
            transitionFirst: false);

        signalledAtSelect.ShouldBeFalse();
        signalledAtStart.ShouldBeTrue();
    }
}
