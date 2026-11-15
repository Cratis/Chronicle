// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_the_catalog_becomes_available : given.an_initializer
{
    async Task Establish() => await Create(false).Initialize(_name);

    async Task Because() => _result = await Create().Initialize(_name);
    [Fact] void should_enable() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] async Task should_resolve_the_unavailable_blocker() => (await _audit.GetBlockers()).Any(_ => _.ResolvedAt is null).ShouldBeFalse();
    [Fact] void should_adopt_every_materialization() => _catalog.Records.Count.ShouldEqual(2);
}
