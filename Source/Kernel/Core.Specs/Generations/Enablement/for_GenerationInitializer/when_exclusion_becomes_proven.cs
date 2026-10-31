// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_exclusion_becomes_proven : given.an_initializer
{
    async Task Establish()
    {
        _proof.IsProven(_name).Returns(false);
        await _initializer.Initialize(_name);
        _proof.IsProven(_name).Returns(true);
    }

    async Task Because() => _result = await Create().Initialize(_name);

    [Fact] void should_enable_after_proof() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] async Task should_retain_resolved_refusal() => (await _audit.GetBlockers()).Single().ResolvedAt.ShouldNotBeNull();
}
