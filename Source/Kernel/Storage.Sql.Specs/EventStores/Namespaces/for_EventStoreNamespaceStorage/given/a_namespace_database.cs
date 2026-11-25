// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage.given;

public class a_namespace_database : for_Database.given.a_database_without_read_model_storage
{
    protected string NamespaceConnection => new SqliteConnectionStringBuilder(_connectionString)
    {
        DataSource = Path.Combine(_directory, "cluster+es+store+namespace.db"),
        Pooling = false
    }.ConnectionString;

    protected Task<bool> HasData() => _database.NamespaceHasEvents("store", "namespace");
}
