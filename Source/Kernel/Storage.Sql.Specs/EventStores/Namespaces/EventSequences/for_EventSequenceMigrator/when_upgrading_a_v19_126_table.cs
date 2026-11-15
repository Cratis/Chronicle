// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_v19_126_table : given.a_v19_126_table
{
    int _preserved;

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        _preserved = await PreservedRows();
    }

    [Fact] void should_add_nullable_history_columns_without_changing_existing_rows() => _preserved.ShouldEqual(1);
}
