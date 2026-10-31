// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_dropping_during_a_replay : Specification
{
    static readonly SinkCollectionNames _names = SinkCollectionNames.For("things");

    [Fact] void should_allow_dropping_the_replay_shadow() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotDropDuringReplay(_names, "replay-things")).ShouldBeNull();
    [Fact] void should_allow_dropping_the_promoting_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotDropDuringReplay(_names, "replay-things-promoting")).ShouldBeNull();
    [Fact] void should_refuse_dropping_the_primary() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotDropDuringReplay(_names, "things")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_dropping_another_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfCannotDropDuringReplay(_names, "things-20261021-abcdef12")).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
