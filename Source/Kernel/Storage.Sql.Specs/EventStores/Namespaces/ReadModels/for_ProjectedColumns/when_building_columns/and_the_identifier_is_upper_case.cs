// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_the_identifier_is_upper_case : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "ID": { "type": "string" }, "Name": { "type": "string" } } }""");

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact] void should_have_one_key_column() => _result.Count(column => column.IsKey).ShouldEqual(1);
    [Fact] void should_name_the_key_after_the_identifier() => _result.Single(column => column.IsKey).Name.ShouldEqual("ID");
    [Fact] void should_put_the_key_first() => _result[0].Name.ShouldEqual("ID");
    [Fact] void should_type_the_key_from_the_schema() => _result.Single(column => column.IsKey).ClrType.ShouldEqual(typeof(string));
    [Fact] void should_not_allow_null_in_the_key() => _result.Single(column => column.IsKey).IsNullable.ShouldBeFalse();
    [Fact] void should_not_have_an_Id_column() => _result.Any(column => column.Name == "Id").ShouldBeFalse();
}
