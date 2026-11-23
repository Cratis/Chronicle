// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_using_postgresql(PostgreSqlFixture fixture) : given.a_provenance_database
{
    string _primary;
    string _independent;

    async Task Because()
    {
        _primary = await fixture.CreateDatabase();
        _independent = await fixture.CreateDatabase();
        await Execute(_primary, "CREATE SCHEMA alternate");
        var alias = new NpgsqlConnectionStringBuilder(_primary) { SearchPath = "alternate" };
        await Exercise(_primary, _independent, alias.ConnectionString);
    }

    async Task Destroy()
    {
        await fixture.DropDatabase(_primary);
        await fixture.DropDatabase(_independent);
    }
}
