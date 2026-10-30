// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_catalog_is_unavailable : given.an_initializer
{
    async Task Because() => _result = await Create(false).Initialize(_name);
    [Fact] async Task should_record_unavailable() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.CatalogUnavailable);
    [Fact] void should_not_enable() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
}
