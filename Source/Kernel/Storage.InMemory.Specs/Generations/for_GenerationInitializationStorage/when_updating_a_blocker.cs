// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationInitializationStorage;

public class when_updating_a_blocker : given.an_initialization_storage
{
    readonly DateTimeOffset _raised = DateTimeOffset.FromUnixTimeSeconds(10000);
    GenerationInitializationBlocker _persisted;

    async Task Because()
    {
        await _audit.RecordBlocker(Blocker(_raised));
        await _audit.RecordBlocker(Blocker(DateTimeOffset.UtcNow) with { Detail = "still blocked" });
        _persisted = (await _audit.GetBlockers()).Single();
    }
    [Fact] void should_preserve_raised_time() => _persisted.RaisedAt.ShouldEqual(_raised);
    [Fact] void should_update_detail() => _persisted.Detail.ShouldEqual("still blocked");
    [Fact] void should_remain_unresolved() => _persisted.ResolvedAt.ShouldBeNull();
}
