// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_schema_has_pii : Specification
{
    IReadOnlyList<ProjectedColumn> _result;

    void Because()
    {
        var schema = JsonSchema.FromJson(
            """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "name": {
                  "type": "string",
                  "compliance": [{ "metadataType": "PII", "details": "" }]
                }
              }
            }
            """);
        _result = ProjectedColumns.ForSchema(schema);
    }

    [Fact] void should_include_the_default_subject_column() => _result.ShouldContain(_ => _.Name == WellKnownProperties.Subject && !_.IsJson);
    [Fact] void should_include_the_per_property_subject_map_as_json() => _result.ShouldContain(_ => _.Name == WellKnownProperties.Subjects && _.IsJson);
}
