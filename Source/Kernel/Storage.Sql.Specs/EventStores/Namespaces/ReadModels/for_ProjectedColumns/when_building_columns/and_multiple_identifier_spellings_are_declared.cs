// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_multiple_identifier_spellings_are_declared : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "ID": { "type": "string" }, "id": { "type": "string" }, "Id": { "type": "string" } } }""");

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact] void should_preserve_the_existing_pascal_case_precedence() => _result.Single(column => column.IsKey).Name.ShouldEqual("Id");
}
