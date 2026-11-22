// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;
using Cratis.Monads;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public class when_signalling_v2_transition_for_sql_server : Specification
{
    bool _signalled;

    async Task Because()
    {
        var interleaving = new V2Interleaving();
        await interleaving.Run(() => Task.FromResult(true), async () =>
        {
            using var connection = new SqlConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM UniqueConstraintAuthoritiesV2";
            await interleaving.Commands.ReaderExecutingAsync(command, null!, default);
            _signalled = interleaving.TransitionWasAttempted;
            return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(new(new(Guid.NewGuid()), UniqueConstraintWritePhase.Live, new(0)));
        }, writerFirst: false);
    }

    [Fact] void should_signal_at_the_authority_select() => _signalled.ShouldBeTrue();
}
