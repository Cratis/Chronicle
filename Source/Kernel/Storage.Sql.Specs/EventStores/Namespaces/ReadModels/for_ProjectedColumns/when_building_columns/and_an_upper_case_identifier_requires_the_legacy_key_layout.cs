// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_an_upper_case_identifier_requires_the_legacy_key_layout : Specification
{
    JsonSchema _schema;
    ProjectedColumn _existingPrimaryKey;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish()
    {
        // PostgreSQL permits distinct Id and ID columns; SQLite cannot reproduce that physical layout.
        _existingPrimaryKey = new ProjectedColumn("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false);
        _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "ID": { "type": "string" } } }""");
    }

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact] void should_keep_the_key_used_by_existing_tables() => _result.Single(column => column.IsKey).ShouldEqual(_existingPrimaryKey);
    [Fact] void should_leave_the_schema_identifier_as_the_legacy_nullable_column() => _result.Single(column => column.Name == "ID").IsNullable.ShouldBeTrue();
}
