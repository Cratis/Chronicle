// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public abstract class batch_parity_scenarios : a_provider_batch_storage
{
    protected async Task CheckParity()
    {
        foreach (var mode in new[] { UniqueConstraintMode.PerEventSource, UniqueConstraintMode.PerValue })
        {
            await CheckOrdered(mode);
            await CheckChunks(mode);
            await CheckOrdinal(mode);
            if (_sqlServer) await CheckPadding(mode);
        }
    }

    async Task CheckOrdered(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await Seed();
        await CheckClaims([new("owner", "held"), new("other", "held"), new("other", "free"), new("owner", "free"), new("moving", "old")]);
        await CheckClaims([]);
        await CheckChanges([]);
        await CheckChanges([
            Save("claimant", "held"), Save("owner", "held", subject: "updated"),
            Save("moving", "new"), Remove("released"), Save("taker", "release"),
            Save("owner", "held", scope: "other"), Save("scoped-taker", "held", scope: "other"),
            Remove("owner"), Save("claimant", "held"), Save("moving", "new"),
            RemoveValue("moving", "old"), RemoveValue("claimant", "held"),
            Save("next", "held"), new(UniqueConstraintIndexChangeKind.Save, "nothing", 1UL, "")]);
        await CheckClaims([new("other", "old"), new("other", "new"), new("next", "held"), new("other", "release")]);
        await CheckChanges([Save("racer", "held"), Remove("moving"), Save("other", "new")]);
    }

    async Task CheckChunks(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await Seed();
        var changes = Enumerable.Range(0, 410).Select(index => Save($"source-{index}", $"value-{index}")).ToList();
        changes.AddRange([Save("loser", "held"), Remove("owner"), Save("winner", "held"), Save("other", "scoped", scope: "other")]);
        await CheckChanges(changes);
        await CheckClaims(Enumerable.Range(0, 410).Select(index => new UniqueConstraintClaim($"source-{index}", $"value-{index}")).ToArray());
        await CheckChanges(Enumerable.Range(0, 205).Select(index => Remove($"source-{index}")).ToArray());
    }

    async Task CheckOrdinal(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await CheckChanges([Save("Alpha", "cafe", 31UL, subject: "first"), Save("alpha", "café", 32UL, subject: "accent"), Save("Ａlpha", "CAFÉ", 33UL, subject: "width"), Save("loser", "cafe")]);
        await CheckClaims([new("Alpha", "cafe"), new("alpha", "cafe"), new("alpha", "café"), new("Alpha", "café"), new("Ａlpha", "CAFÉ"), new("alpha", "CAFE")]);
        await CheckChanges([Save("alpha", "new", 40UL, subject: "changed"), RemoveValue("alpha", "cafe"), Remove("Alpha"), Save("next", "cafe")]);
    }

    async Task CheckPadding(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await CheckChanges([Save("input ", "padded "), Save("input", "padded"), Save("claimant", "padded")]);
        await CheckClaims([new("input ", "padded "), new("input", "padded"), new("other", "padded")]);
        await Prepare(mode);
        foreach (var storage in new[] { _storage, _oracle }) await storage.Save("stored ", _definition, 7UL, "value ", subject: "legacy");
        await CheckClaims([new("stored", "value"), new("other", "value")]);
        await CheckChanges([Save("stored", "value", 8UL, subject: "updated"), Remove("stored"), Save("next", "value")]);
        await CheckChanges([RemoveValue("other", "value"), RemoveValue("next", "value")]);
    }
}
