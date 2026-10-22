// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_moving_the_primary_aside : Specification
{
    static readonly SinkCollectionNames _names = SinkCollectionNames.For("things");

    [Fact] void should_allow_moving_it_to_a_revert_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotMovePrimaryAside(_names, "things-20261021-abcdef12")).ShouldBeNull();
    [Fact] void should_refuse_moving_it_onto_itself() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotMovePrimaryAside(_names, "things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_moving_it_onto_the_replay_shadow() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotMovePrimaryAside(_names, "replay-things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_moving_it_onto_the_promoting_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotMovePrimaryAside(_names, "replay-things-promoting")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
