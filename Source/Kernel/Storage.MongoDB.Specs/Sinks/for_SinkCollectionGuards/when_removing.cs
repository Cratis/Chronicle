// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_removing : Specification
{
    static readonly SinkCollectionNames _names = SinkCollectionNames.For("things");

    [Fact] void should_allow_removing_a_revert_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotRemove(_names, "things-20261021-abcdef12")).ShouldBeNull();
    [Fact] void should_allow_removing_the_primary() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotRemove(_names, "things")).ShouldBeNull();
    [Fact] void should_refuse_removing_the_replay_shadow() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotRemove(_names, "replay-things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_removing_the_promoting_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotRemove(_names, "replay-things-promoting")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
