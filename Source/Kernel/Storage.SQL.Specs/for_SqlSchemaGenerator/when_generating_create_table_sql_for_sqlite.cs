// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Storage.SQL.Models;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.SQL.for_SqlSchemaGenerator;

public class when_generating_create_table_sql_for_sqlite : Specification
{
    SqlSchemaGenerator generator;
    Model model;
    string result;

    void Establish()
    {
        generator = new SqlSchemaGenerator(SqlProviderType.SQLite, "main");
        
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Properties =
            {
                ["Name"] = new JsonSchema { Type = JsonObjectType.String },
                ["Age"] = new JsonSchema { Type = JsonObjectType.Integer },
                ["IsActive"] = new JsonSchema { Type = JsonObjectType.Boolean },
                ["Score"] = new JsonSchema { Type = JsonObjectType.Number }
            }
        };

        model = new Model(new ModelName("TestModel"), schema);
    }

    void Because() => result = generator.GenerateCreateTableSql(model);

    [Fact] void should_generate_create_table_statement() => result.ShouldContain("CREATE TABLE IF NOT EXISTS");
    [Fact] void should_include_table_name() => result.ShouldContain("\"projection_testmodel\"");
    [Fact] void should_include_id_column() => result.ShouldContain("Id TEXT PRIMARY KEY");
    [Fact] void should_include_event_sequence_number_column() => result.ShouldContain("EventSequenceNumber BIGINT NOT NULL");
    [Fact] void should_include_last_updated_column() => result.ShouldContain("LastUpdated TIMESTAMP DEFAULT (datetime('now'))");
    [Fact] void should_include_data_column() => result.ShouldContain("Data TEXT");
    [Fact] void should_include_name_column() => result.ShouldContain("\"Name\" TEXT");
    [Fact] void should_include_age_column() => result.ShouldContain("\"Age\" BIGINT");
    [Fact] void should_include_is_active_column() => result.ShouldContain("\"IsActive\" INTEGER");
    [Fact] void should_include_score_column() => result.ShouldContain("\"Score\" REAL");
}