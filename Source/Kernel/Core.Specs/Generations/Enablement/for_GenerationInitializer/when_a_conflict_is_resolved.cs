// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_a_conflict_is_resolved : given.an_initializer
{
    async Task Establish()
    {
        _catalog.Conflict = new("projection-b");
        await _initializer.Initialize(_name);
        _catalog.Conflict = null;
    }

    async Task Because() => _result = await Create().Initialize(_name);

    [Fact] void should_enable() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] async Task should_preserve_resolved_conflict() => (await _audit.GetBlockers()).Single().ResolvedAt.ShouldNotBeNull();
}
