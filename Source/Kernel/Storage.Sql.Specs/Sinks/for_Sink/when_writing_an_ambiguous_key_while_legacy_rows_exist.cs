// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_an_ambiguous_key_while_legacy_rows_exist : given.a_sink_with_composite_keys
{
    async Task Because()
    {
        await _harness.StoreRowUnderId("a_b_c", "legacy");
        await Write(KeyOf("a_b", "c"), "new");
    }

    [Fact] async Task should_create_a_new_row() => (await _harness.ReadStoredRows()).Length.ShouldEqual(2);
    [Fact] async Task should_leave_the_legacy_row_alone() => (await _harness.ReadStoredRows()).Select(row => row["name"]).Contains("legacy").ShouldBeTrue();
    [Fact] void should_recommend_a_replay() => _recommender.Received(1).Recommend(Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(), Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition>());
}
