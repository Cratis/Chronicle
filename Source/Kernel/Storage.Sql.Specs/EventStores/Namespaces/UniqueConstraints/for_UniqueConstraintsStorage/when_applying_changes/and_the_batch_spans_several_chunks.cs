// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using FactAttribute = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given.sqlite.FactAttribute;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_applying_changes;

public class and_the_batch_spans_several_chunks : given.a_batch_storage
{
    async Task Because()
    {
        await Check(UniqueConstraintMode.PerValue);
        await Check(UniqueConstraintMode.PerEventSource);
    }

    [Fact] void should_match_changes_across_chunks_in_both_modes() => VerifyChanges();
    [Fact] void should_match_claims_across_chunks_in_both_modes() => VerifyClaims();
    [Fact] void should_stay_below_the_lowest_provider_parameter_limit() => UniqueConstraintsStorage.MaxParametersPerStatement.ShouldBeLessThan(999);

    async Task Check(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        const int limit = UniqueConstraintsStorage.MaxParametersPerStatement;
        foreach (var size in new[] { limit - 1, limit, limit + 1, (limit * 2) + 1 })
        {
            var changes = Enumerable.Range(0, size).Select(index => Save($"source-{size}-{index}", $"value-{size}-{index}", (ulong)index)).ToList();
            changes.Insert(Math.Min(size, limit), Save("duplicate-owner", $"value-{size}-0"));
            await CheckChanges(changes);
            await CheckClaims(changes.Select(change => new UniqueConstraintClaim(change.EventSourceId, change.Value!)).ToArray());
        }
    }
}
