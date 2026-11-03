// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_legacy_key_columns;

public class and_the_existing_key_is_Id : Specification
{
    JsonSchema _schema;
    ProjectedColumn _existingPrimaryKey;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish()
    {
        // PostgreSQL permits distinct Id and ID columns; earlier releases created that layout for a schema spelling ID.
        _existingPrimaryKey = new ProjectedColumn("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false);
        _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "ID": { "type": "string" } } }""");
    }

    void Because() => _result = ProjectedColumns.ForLegacyKey(_schema, [], "Id");

    [Fact] void should_keep_the_key_used_by_existing_tables() => _result.Single(column => column.IsKey).ShouldEqual(_existingPrimaryKey);
    [Fact] void should_put_the_key_first() => _result[0].Name.ShouldEqual("Id");
    [Fact] void should_leave_the_schema_identifier_as_the_legacy_nullable_column() => _result.Single(column => column.Name == "ID").IsNullable.ShouldBeTrue();
    [Fact] void should_not_make_the_schema_identifier_a_key() => _result.Single(column => column.Name == "ID").IsKey.ShouldBeFalse();
}
