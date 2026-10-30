// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_legacy_work_is_drained : given.an_initializer
{
    async Task Establish()
    {
        _legacy.Inspect(_name).Returns([new GenerationInitializationBlocker(_name, GenerationInitializationBlockerKind.UnknownLegacyState, "tenant/job", "unknown status", DateTimeOffset.UtcNow, null)]);
        await _initializer.Initialize(_name);
        _legacy.Inspect(_name).Returns([]);
    }

    async Task Because() => _result = await Create().Initialize(_name);

    [Fact] void should_enable_after_work_is_reconciled() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] async Task should_retain_resolved_work_refusal() => (await _audit.GetBlockers()).Single().ResolvedAt.ShouldNotBeNull();
}
