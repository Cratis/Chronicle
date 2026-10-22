// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Adopts legacy data under serialized, resumable, fail-closed initialization.
/// </summary>
/// <param name="enablement">The operational flag.</param>
/// <param name="storage">The durable store.</param>
/// <param name="exclusion">External writer exclusion proof.</param>
/// <param name="membership">Current membership evidence.</param>
/// <param name="legacyWork">Legacy work inspection.</param>
/// <param name="materializations">Existing materializations.</param>
/// <param name="binarySupport">The executing binary support, defaulting to the bridge contract through DI.</param>
/// <param name="catalog">A1's optional catalog seam.</param>
public sealed class GenerationInitializer(
    IGenerationEnablement enablement,
    IStorage storage,
    IGenerationExclusionProof exclusion,
    IGenerationMembershipProbe membership,
    IGenerationLegacyWork legacyWork,
    ILegacyMaterializations materializations,
    GenerationBinarySupport binarySupport,
    IGenerationCatalogSeam? catalog = null) : Grain, IGenerationInitializer
{
    /// <inheritdoc/>
    public async Task<GenerationInitializationResult> Initialize(EventStoreName eventStore)
    {
        if (!enablement.Enabled)
        {
            return new GenerationInitializationDisabled();
        }
        var store = storage.GetEventStore(eventStore);
        var audit = store.GenerationInitialization;
        var floor = await store.GenerationFloor.Get();
        if (floor is null || floor.Revision == 0 || floor.Incarnation.Value == Guid.Empty ||
            floor.MinimumStorageProtocol.Value == 0 || floor.MinimumWriterProtocol.Value == 0 ||
            !binarySupport.Satisfies(floor) ||
            !floor.RequiredCapabilities.Contains(GenerationCapability.InPlaceAdoption) ||
            !floor.RequiredCapabilities.Contains(GenerationCapability.BindingEpoch))
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.FloorNotEstablished, "store", "A durable nonzero floor is required.");
        }
        var receipts = await audit.GetReceipts();
        if (receipts.Any(_ => _.EventStore != eventStore || _.Incarnation != floor.Incarnation || _.FloorRevision > floor.Revision))
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, "store", "Initialization receipts do not match the durable floor.");
        }
        if (receipts.Any(_ => _.Step == GenerationInitializationStep.Enabled && _.Subject == "store" && _.EventStore == eventStore && _.Incarnation == floor.Incarnation && _.FloorRevision <= floor.Revision) && !(await audit.GetBlockers()).Any(_ => _.ResolvedAt is null))
        {
            return new GenerationInitializationAlreadyEnabled();
        }
        if (!await exclusion.IsProven(eventStore))
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.ExclusionNotProven, "store", "Old-writer exclusion has not been proven.");
        }
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.ExclusionNotProven, "store", DateTimeOffset.UtcNow);
        var members = await membership.Probe(floor);
        if (!members.IsUniform)
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.MembershipNotUniform, "store", members.Reason);
        }
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.MembershipNotUniform, "store", DateTimeOffset.UtcNow);
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.FloorNotEstablished, "store", DateTimeOffset.UtcNow);
        await Record(audit, eventStore, floor, GenerationInitializationStep.FloorVerified, "store", members.Reason);
        IReadOnlyList<GenerationInitializationBlocker> blockers;
        try
        {
            blockers = await legacyWork.Inspect(eventStore);
        }
        catch (InvalidOperationException error)
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.UnknownLegacyState, "legacy-work", error.Message);
        }
        foreach (var blocker in blockers)
        {
            await audit.RecordBlocker(blocker);
        }
        if (blockers.Count != 0)
        {
            return new GenerationInitializationBlocked(blockers);
        }
        foreach (var blocker in (await audit.GetBlockers()).Where(_ => _.ResolvedAt is null && IsLegacyWorkRefusal(_)))
        {
            await audit.ResolveBlocker(blocker.Kind, blocker.Subject, DateTimeOffset.UtcNow);
        }
        await Record(audit, eventStore, floor, GenerationInitializationStep.LegacyWorkReconciled, "store", "No unresolved legacy replay work.");
        if (catalog is null)
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.CatalogUnavailable, "store", "The generation catalog is unavailable.");
        }
        LegacyMaterialization[] snapshot;
        try
        {
            snapshot = (await materializations.GetAll(eventStore)).OrderBy(Subject, StringComparer.Ordinal).ToArray();
        }
        catch (InvalidOperationException error)
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.UnknownLegacyState, "store", error.Message);
        }
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.UnknownLegacyState, "store", DateTimeOffset.UtcNow);
        receipts = await audit.GetReceipts();
        var adoptedReceipts = receipts.Where(receipt => receipt.Step == GenerationInitializationStep.MaterializationAdopted).ToDictionary(receipt => receipt.Subject);
        if (adoptedReceipts.Keys.Except(snapshot.Select(Subject)).Any())
        {
            return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, "store", "A previously adopted materialization is missing.");
        }
        var createdAt = receipts.Single(receipt => receipt.Step == GenerationInitializationStep.FloorVerified && receipt.Subject == "store").RecordedAt;
        foreach (var legacy in snapshot)
        {
            var subject = Subject(legacy);
            if (adoptedReceipts.TryGetValue(subject, out var receipt))
            {
                if (receipt.Evidence != Evidence(legacy))
                {
                    return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, "The materialization snapshot no longer matches its receipt.");
                }
            }
            else
            {
                var identity = MaterializationGenerationIdentity.For(legacy.Projection, MaterializationGeneration.First, legacy.Container);
                if (legacy.Observer != identity.ObserverId || legacy.FailedPartitions < 0)
                {
                    return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, "The legacy observer contradicts generation-one identity.");
                }
                var requested = new MaterializationGenerationRecord(identity, MaterializationGenerationState.Active, MaterializationRevision.Unknown, createdAt);
                if (await catalog.TryAdoptFirst(eventStore, legacy.Namespace, requested) == AdoptResult.Conflicting)
                {
                    return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, "The existing reservation contradicts legacy adoption.");
                }
                await Record(audit, eventStore, floor, GenerationInitializationStep.MaterializationAdopted, subject, Evidence(legacy));
            }
            await audit.ResolveBlocker(GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, DateTimeOffset.UtcNow);
        }
        foreach (var legacy in snapshot)
        {
            var subject = Subject(legacy);
            if (receipts.Any(receipt => receipt.Step == GenerationInitializationStep.BindingEpochSet && receipt.Subject == subject))
            {
                continue;
            }
            if (!await catalog.TrySetBindingEpoch(eventStore, legacy.Namespace, legacy.Projection, 0, 1))
            {
                return await Block(audit, eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, "The existing binding contradicts the initial epoch.");
            }
            await Record(audit, eventStore, floor, GenerationInitializationStep.BindingEpochSet, subject, "Epoch=1");
            await audit.ResolveBlocker(GenerationInitializationBlockerKind.ContradictoryLegacyState, subject, DateTimeOffset.UtcNow);
        }
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.ContradictoryLegacyState, "store", DateTimeOffset.UtcNow);
        await audit.ResolveBlocker(GenerationInitializationBlockerKind.CatalogUnavailable, "store", DateTimeOffset.UtcNow);
        var unresolved = (await audit.GetBlockers()).Where(_ => _.ResolvedAt is null).ToArray();
        if (unresolved.Length != 0)
        {
            return new GenerationInitializationBlocked(unresolved);
        }
        await Record(audit, eventStore, floor, GenerationInitializationStep.Enabled, "store", "Legacy adoption complete.");
        return new GenerationInitializationEnabled(await audit.GetReceipts());
    }

    static bool IsLegacyWorkRefusal(GenerationInitializationBlocker blocker) =>
        blocker.Kind is GenerationInitializationBlockerKind.LegacyReplaySessionUnresolved or GenerationInitializationBlockerKind.LegacyReplayJobUnresolved ||
        (blocker.Kind == GenerationInitializationBlockerKind.UnknownLegacyState && blocker.Subject != "store") ||
        (blocker.Kind == GenerationInitializationBlockerKind.ContradictoryLegacyState && blocker.Subject.StartsWith("legacy-work/", StringComparison.Ordinal));

    static string Subject(LegacyMaterialization legacy) => $"{legacy.Namespace.Value.Length}:{legacy.Namespace}/{legacy.Projection}";

    static string Evidence(LegacyMaterialization legacy) => $"Container={legacy.Container};Observer={legacy.Observer};Position={legacy.PositionEvidence};FailedPartitions={legacy.FailedPartitions};Revision=Unknown";

    static Task<bool> Record(IGenerationInitializationStorage audit, EventStoreName store, GenerationFloor floor, GenerationInitializationStep step, string subject, string evidence) =>
        audit.TryRecord(new(store, step, subject, evidence, floor.Incarnation, floor.Revision, DateTimeOffset.UtcNow));

    static async Task<GenerationInitializationResult> Block(IGenerationInitializationStorage audit, EventStoreName store, GenerationInitializationBlockerKind kind, string subject, string detail)
    {
        await audit.RecordBlocker(new(store, kind, subject, detail, DateTimeOffset.UtcNow, null));
        return new GenerationInitializationBlocked((await audit.GetBlockers()).Where(_ => _.ResolvedAt is null).ToArray());
    }
}
