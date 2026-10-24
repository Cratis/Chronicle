// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_clearing : Specification
{
    static readonly SinkCollectionNames _names = SinkCollectionNames.For("things");

    [Fact] void should_allow_clearing_the_replay_shadow_during_a_replay() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotClear(_names, true, "replay-things")).ShouldBeNull();
    [Fact] void should_allow_clearing_the_primary_outside_a_replay() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotClear(_names, false, "things")).ShouldBeNull();
    [Fact] void should_refuse_clearing_the_primary_during_a_replay() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotClear(_names, true, "things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_clearing_the_replay_shadow_outside_a_replay() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotClear(_names, false, "replay-things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_clearing_another_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotClear(_names, true, "others")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
