// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(PostgreSqlCollection.Name)]
public class and_it_runs_inside_another(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    bool _innerChanged;

    Task Because() => DatabaseSchemaLock.Run(
        NewContext(),

        // A short timeout makes a lock that waits for itself fail instead of hanging.
        () => DatabaseSchemaLock.Run(
            NewContext(),
            () =>
            {
                _innerChanged = true;
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(300)));

    [Fact] void should_run_the_inner_change() => _innerChanged.ShouldBeTrue();
}
