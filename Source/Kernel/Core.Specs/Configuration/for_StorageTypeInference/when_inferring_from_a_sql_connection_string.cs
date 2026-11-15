// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration.for_StorageTypeInference;

public class when_inferring_from_a_sql_connection_string : Specification
{
    string? _postgreSql;
    string? _msSql;
    string? _sqlite;

    void Because()
    {
        _postgreSql = StorageTypeInference.Infer("Host=pg;Database=chronicle;Username=chronicle");
        _msSql = StorageTypeInference.Infer("Server=sql;Database=chronicle;User Id=chronicle");
        _sqlite = StorageTypeInference.Infer("Data Source=chronicle.db");
    }

    [Fact] void should_infer_postgresql() => _postgreSql.ShouldEqual(StorageType.PostgreSql);
    [Fact] void should_infer_mssql() => _msSql.ShouldEqual(StorageType.MsSql);
    [Fact] void should_infer_sqlite() => _sqlite.ShouldEqual(StorageType.Sqlite);
}
