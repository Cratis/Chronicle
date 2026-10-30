// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_using_default_bridge_support : given.an_initializer
{
    async Task Because() => _result = await Create(bridgeSupport: true).Initialize(_name);

    [Fact] void should_refuse_the_future_floor() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] async Task should_keep_a_durable_floor_blocker() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.FloorNotEstablished);
    [Fact] async Task should_write_no_receipt() => (await _audit.GetReceipts()).ShouldBeEmpty();
    [Fact] void should_not_call_catalog() => _catalog.Calls.ShouldEqual(0);
    [Fact] void should_not_adopt() => _catalog.Records.ShouldBeEmpty();
}
