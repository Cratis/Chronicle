// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Storage.SQL.Models;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.SQL.for_SqlSchemaGenerator;

public class when_generating_create_table_sql_for_sql_server : Specification
{
    SqlSchemaGenerator generator;
    JsonSchema schema;
    Model model;
    string sql;

    void Establish()
    {
        generator = new SqlSchemaGenerator(SqlProviderType.SqlServer, "dbo");
        schema = JsonSchema.FromType<TestModel>();
        model = new Model("test-model", schema);
    }

    void Because() => sql = generator.GenerateCreateTableSql(model);

    [Fact] void should_contain_create_table_statement() => sql.ShouldContain("CREATE TABLE");
    [Fact] void should_contain_table_name() => sql.ShouldContain("projection_test_model");
    [Fact] void should_contain_primary_key() => sql.ShouldContain("Id NVARCHAR(255) PRIMARY KEY");
}