// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableCatalog.when_building_statements;

public class and_the_schema_is_empty : Specification
{
    [Theory]
    [InlineData(DatabaseType.PostgreSql, "current_schema()")]
    [InlineData(DatabaseType.SqlServer, "SCHEMA_NAME()")]
    void should_fall_back_to_the_default_schema_when_finding(DatabaseType provider, string fallback) =>
        ReadModelTableCatalog.FindSql(provider).ShouldContain(fallback);

    [Theory]
    [InlineData(DatabaseType.PostgreSql, "current_schema()")]
    [InlineData(DatabaseType.SqlServer, "SCHEMA_NAME()")]
    void should_fall_back_to_the_default_schema_for_primary_keys(DatabaseType provider, string fallback) =>
        ReadModelTableCatalog.PrimaryKeySql(provider).ShouldContain(fallback);
}
