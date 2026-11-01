// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_short_names_keep_their_bytes : Specification
{
    [Theory]
    [InlineData(DatabaseType.PostgreSql)]
    [InlineData(DatabaseType.SqlServer)]
    [InlineData(DatabaseType.Sqlite)]
    void should_keep_every_short_name_unchanged(DatabaseType provider)
    {
        foreach (var name in new[] { "orders", "replay-orders", "orders-20260101120000-1a2b3c4d", new string('x', 48) + "-20260101120000", new string('x', 39) + "-20260101120000-1a2b3c4d" })
        {
            ReadModelTableNames.Candidate(provider, name).ShouldEqual(name);
        }
    }
}
