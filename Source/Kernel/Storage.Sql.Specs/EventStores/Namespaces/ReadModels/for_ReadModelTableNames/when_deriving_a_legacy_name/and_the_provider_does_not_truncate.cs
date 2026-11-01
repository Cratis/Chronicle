// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_legacy_name;

public class and_the_provider_does_not_truncate : Specification
{
    [Theory]
    [InlineData(DatabaseType.SqlServer)]
    [InlineData(DatabaseType.Sqlite)]
    void should_keep_the_legacy_name_without_a_fallback(DatabaseType provider)
    {
        var name = new string('x', 300);
        ReadModelTableNames.Legacy(provider, name).ShouldEqual(name);
        ReadModelTableNames.HasLegacyName(provider, name).ShouldBeFalse();
    }
}
