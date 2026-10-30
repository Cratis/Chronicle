// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationInitializationStorage.when_resolving_a_blocker;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_initialization_storage(fixture)
{
    readonly DateTimeOffset _resolved = DateTimeOffset.FromUnixTimeSeconds(20000);
    GenerationInitializationBlocker _persisted;

    async Task Because()
    {
        await _audit.RecordBlocker(Blocker(_resolved.AddMinutes(-5)));
        await _audit.ResolveBlocker(GenerationInitializationBlockerKind.ExclusionNotProven, "store", _resolved);
        _persisted = (await _audit.GetBlockers()).Single();
    }
    [Fact] void should_retain_audit() => _persisted.Kind.ShouldEqual(GenerationInitializationBlockerKind.ExclusionNotProven);
    [Fact] void should_stamp_resolution() => _persisted.ResolvedAt.ShouldEqual(_resolved);
}
