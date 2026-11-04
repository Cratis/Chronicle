// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ProjectedColumns.when_building_columns;

public class and_the_identifier_is_pascal_case : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _result;

    void Establish() => _schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "Name": { "type": "string" }, "Id": { "type": "string" } } }""");

    void Because() => _result = ProjectedColumns.ForSchema(_schema);

    [Fact]
    void should_keep_the_columns_as_they_have_always_been() => string.Join(',', _result.Select(column => $"{column.Name}:{column.IsKey}:{column.IsNullable}")).ShouldEqual(
        "Id:True:False,Name:False:True,__lastHandledEventSequenceNumber:False:True,__initialized:False:False");
}
