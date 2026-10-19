// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_looking_up_an_ambiguous_key_while_legacy_rows_exist : given.a_sink_with_composite_keys
{
    Exception? _error;

    async Task Because()
    {
        await _harness.StoreRowUnderId("a_b_c", "legacy");
        _error = await Catch.Exception(async () => await _sink.FindOrDefault(KeyOf("a_b", "c")));
    }

    [Fact] void should_refuse_the_read() => _error.ShouldBeOfExactType<LegacyCompositeKeyRequiresReplay>();
    [Fact] void should_recommend_a_replay() => _recommender.Received(1).Recommend(Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(), Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition>());
}
