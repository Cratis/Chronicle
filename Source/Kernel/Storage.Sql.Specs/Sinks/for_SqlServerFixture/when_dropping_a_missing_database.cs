// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerFixture;

[Collection(SqlServerCollection.Name)]
public class when_dropping_a_missing_database(SqlServerFixture fixture) : Specification
{
    string _database;
    Exception? _exception;

    async Task Establish()
    {
        _database = await fixture.CreateDatabase();
        await fixture.DropDatabase(_database);
    }

    async Task Because() => _exception = await Catch.Exception(() => fixture.DropDatabase(_database));

    [Fact] void should_propagate_the_deletion_failure_to_the_caller() => _exception.ShouldBeOfExactType<SqlException>();
}
