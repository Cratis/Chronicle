// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_the_namespace_database_cannot_be_read : given.a_namespace_database
{
    Exception _error;

    Task Establish() => File.WriteAllTextAsync(new SqliteConnectionStringBuilder(NamespaceConnection).DataSource, "not a sqlite database");

    async Task Because() => _error = await Catch.Exception(HasData);

    [Fact] void should_not_report_corrupt_storage_as_empty() => _error.ShouldBeOfExactType<SqliteException>();
}
