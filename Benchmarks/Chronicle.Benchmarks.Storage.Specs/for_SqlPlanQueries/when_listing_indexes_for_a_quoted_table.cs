// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlPlanQueries;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_listing_indexes_for_a_quoted_table : given.a_sqlite_index_catalog
{
    async Task Because() => _indexes = await _queries.IndexDefinitions(new("SELECT * FROM \"events\"", []));

    [Fact] void should_find_the_table_indexes() => _indexes.ShouldContain("CREATE INDEX events_sequence_number");
}
