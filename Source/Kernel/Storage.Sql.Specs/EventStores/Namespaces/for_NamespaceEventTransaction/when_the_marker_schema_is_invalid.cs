// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction;

public class when_the_marker_schema_is_invalid : given.a_namespace_transaction
{
    Exception? _failure;

    async Task Establish()
    {
        await _markers.TryOpen(_incarnation, null);
        await using var scope = await _database.Namespace("fenced-store", "fenced-namespace");
        await scope.DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE \"NamespaceMarker\" RENAME COLUMN \"IsOpen\" TO \"MissingOpen\"");
    }

    async Task Because()
    {
        try
        {
            await _transaction.Execute(_context, WriteReceipt);
        }
        catch (Exception error)
        {
            _failure = error;
        }
    }

    [Fact] void should_not_misclassify_an_invalid_column_as_a_missing_marker() => _failure.ShouldBeOfExactType<SqliteException>();
    [Fact] void should_not_invoke_the_body() => _called.ShouldBeFalse();
}
