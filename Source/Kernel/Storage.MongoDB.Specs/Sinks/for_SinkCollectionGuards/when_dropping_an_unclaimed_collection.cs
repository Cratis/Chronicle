// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_dropping_an_unclaimed_collection : Specification
{
    [Fact] void should_allow_the_operation() =>
        Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(SinkCollectionOperation.Drop, "models-g2", "models-g2", [])).ShouldBeNull();
}
