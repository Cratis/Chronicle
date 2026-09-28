// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_PrimaryKeyNames.when_naming;

public class and_the_name_fits : Specification
{
    string _postgreSql;
    string _sqlServer;
    string _sqlite;

    void Because()
    {
        _postgreSql = PrimaryKeyNames.For(DatabaseType.PostgreSql, "test_read_models-20260101120000");
        _sqlServer = PrimaryKeyNames.For(DatabaseType.SqlServer, "test_read_models-20260101120000");
        _sqlite = PrimaryKeyNames.For(DatabaseType.Sqlite, "test_read_models-20260101120000");
    }

    [Fact] void should_name_it_after_the_table_on_postgresql() => _postgreSql.ShouldEqual("PK_test_read_models-20260101120000");
    [Fact] void should_name_it_after_the_table_on_sql_server() => _sqlServer.ShouldEqual("PK_test_read_models-20260101120000");
    [Fact] void should_name_it_after_the_table_on_sqlite() => _sqlite.ShouldEqual("PK_test_read_models-20260101120000");
}
