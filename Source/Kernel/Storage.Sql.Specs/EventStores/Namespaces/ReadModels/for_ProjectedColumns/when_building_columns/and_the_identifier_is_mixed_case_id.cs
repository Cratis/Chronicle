// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_the_identifier_is_mixed_case_id : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "iD": { "type": "string" } } }""");

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact] void should_keep_the_schemas_identifier_spelling() => _result.Single(column => column.IsKey).Name.ShouldEqual("iD");
    [Fact] void should_not_synthesize_a_second_identifier_column() => _result.Count(column => column.Name.Equals("id", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
}
