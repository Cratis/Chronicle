// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_the_identifier_is_upper_case_id : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "Name": { "type": "string" }, "ID": { "type": "string" } } }""");

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact] void should_use_the_schemas_identifier_as_the_primary_key() => _result.Single(column => column.IsKey).Name.ShouldEqual("ID");
    [Fact] void should_not_synthesize_a_second_identifier_column() => _result.Any(column => column.Name == "Id").ShouldBeFalse();
    [Fact] void should_put_the_key_first() => _result.First().Name.ShouldEqual("ID");
    [Fact] void should_make_the_schema_identifier_non_nullable() => _result.Single(column => column.Name == "ID").IsNullable.ShouldBeFalse();
}
