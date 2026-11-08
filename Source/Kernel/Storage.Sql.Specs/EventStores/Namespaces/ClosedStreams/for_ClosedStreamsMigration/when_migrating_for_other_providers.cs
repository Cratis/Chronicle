// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

public class when_migrating_for_other_providers : Specification
{
    int _sqlite;
    int _postgresql;

    void Because()
    {
        var sqlite = new v20_0_2 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" };
        var postgresql = new v20_0_2 { ActiveProvider = "Npgsql.EntityFrameworkCore.PostgreSQL" };
        _sqlite = sqlite.UpOperations.Count + sqlite.DownOperations.Count;
        _postgresql = postgresql.UpOperations.Count + postgresql.DownOperations.Count;
    }

    [Fact] void should_leave_sqlite_unchanged() => _sqlite.ShouldEqual(0);
    [Fact] void should_leave_postgresql_unchanged() => _postgresql.ShouldEqual(0);
}
