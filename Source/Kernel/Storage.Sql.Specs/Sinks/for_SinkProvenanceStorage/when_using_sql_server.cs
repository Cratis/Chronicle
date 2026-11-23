// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

[Collection(SqlServerCollection.Name)]
public class when_using_sql_server(SqlServerFixture fixture) : given.a_provenance_database
{
    string _primary;
    string _independent;
    Exception? _catalogEquivalentError;

    async Task Because()
    {
        _primary = await fixture.CreateDatabase();
        _independent = await fixture.CreateDatabase();
        var alias = new SqlConnectionStringBuilder(_primary) { ApplicationName = "provenance-alias" };
        await Exercise(_primary, _independent, alias.ConnectionString);
        var reserved = new SinkProvenanceStorage(DatabaseFor(_primary), "store", "namespace", "__cratis_sink_provenance ");
        _catalogEquivalentError = await Catch.Exception(reserved.IsQuarantined);
    }

    [Fact] void should_reserve_catalog_equivalent_names_not_just_ordinal_matches() => _catalogEquivalentError.ShouldBeOfExactType<InvalidOperationException>();

    async Task Destroy()
    {
        await fixture.DropDatabase(_primary);
        await fixture.DropDatabase(_independent);
    }
}
