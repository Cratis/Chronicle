// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Sys;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Providers;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchManager"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="patches">All available <see cref="ICanApplyPatch"/> instances.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="kernelVersion">Optional running kernel version for specifications.</param>
[PreferLocalPlacement]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.PatchManager)]
public class PatchManager(
    IGrainFactory grainFactory,
    IStorage storage,
    IInstancesOf<ICanApplyPatch> patches,
    ILogger<PatchManager> logger,
    SemanticVersion? kernelVersion = null) : Grain<PatchManagerState>, IPatchManager
{
    readonly SemanticVersion _kernelVersion = kernelVersion ?? KernelVersion.Current;

    /// <inheritdoc/>
    public async Task ApplyPatches() => await ApplyPatchesFor(_kernelVersion);

    /// <inheritdoc/>
    public async Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller)
    {
        var knownVersion = KernelVersion.IsKnown(_kernelVersion);
        if (knownVersion && KernelVersion.IsKnown(caller) && _kernelVersion < caller)
        {
            return RelocateToCaller();
        }

        if (!knownVersion)
        {
            logger.UnknownKernelVersion(_kernelVersion);
        }
        else
        {
            var recorded = await storage.System.GetPatchingKernelVersion();
            if (recorded is not null && recorded > _kernelVersion)
            {
                logger.SkippingPatchesForNewerKernel(_kernelVersion, recorded);
                return PatchOutcome.Completed;
            }
        }

        await ApplyPendingPatches();
        if (knownVersion)
        {
            await storage.System.SetPatchingKernelVersion(_kernelVersion);
        }
        return PatchOutcome.Completed;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State is null || State == PatchManagerState.Empty)
        {
            var appliedPatches = await storage.System.Patches.GetAll();
            State = new PatchManagerState(appliedPatches);
            await WriteStateAsync();
        }
        await base.OnActivateAsync(cancellationToken);
    }

    /// <summary>
    /// Filters candidate patches into patches to apply and skipped patches.
    /// </summary>
    /// <param name="candidatePatches">The candidate patches to evaluate.</param>
    /// <param name="alreadyAppliedPatchNames">The names of patches that have already been applied.</param>
    /// <returns>The patches to apply and the skipped count.</returns>
    internal static (IReadOnlyList<ICanApplyPatch> PatchesToApply, int SkippedCount) FilterPatchesToApply(
        IReadOnlyList<ICanApplyPatch> candidatePatches,
        ISet<string> alreadyAppliedPatchNames)
    {
        var patchesToApply = candidatePatches
            .Where(_ => !alreadyAppliedPatchNames.Contains(_.Name))
            .ToList();
        var skippedCount = candidatePatches.Count - patchesToApply.Count;

        return (patchesToApply, skippedCount);
    }

    /// <summary>
    /// Gets the system version after a patch has been handled. Once a patch has been deferred the version must not move
    /// past it, whatever applies after it.
    /// </summary>
    /// <param name="latest">The version recorded so far.</param>
    /// <param name="patch">The patch that was just applied.</param>
    /// <param name="versionBlocked">Whether an earlier patch was deferred.</param>
    /// <returns>The version to record.</returns>
    static SemanticVersion? VersionAfter(SemanticVersion? latest, ICanApplyPatch patch, bool versionBlocked) =>
        versionBlocked ? latest : patch.Version;

    PatchOutcome RelocateToCaller()
    {
        if (PatchManagerRelocation.CallerSilo() is { } callerSilo)
        {
            RequestContext.Set(IPlacementDirector.PlacementHintKey, callerSilo);
            this.MigrateOnIdle();
        }
        else
        {
            DeactivateOnIdle();
        }
        return PatchOutcome.Relocate;
    }

    async Task ApplyPendingPatches()
    {
        logger.StartingPatchApplication();

        var system = grainFactory.GetSystem();
        var currentVersion = await system.GetVersion() ?? SemanticVersion.NotSet;
        logger.CurrentSystemVersion(currentVersion);

        var allPatches = patches.ToList();
        var candidatePatches = allPatches
            .Where(p => p.Version > currentVersion)
            .OrderBy(p => p.Version)
            .ToList();

        if (candidatePatches.Count == 0)
        {
            logger.PatchApplicationCompleted(0, 0);
            return;
        }

        var skippedCount = 0;
        var alreadyAppliedPatchNames = await AppliedPatchNames(candidatePatches);
        var (patchesToApply, computedSkippedCount) = FilterPatchesToApply(candidatePatches, alreadyAppliedPatchNames);
        skippedCount = computedSkippedCount;

        if (patchesToApply.Count > 0)
        {
            logger.FoundPatchesToApply(patchesToApply.Count);
        }

        SemanticVersion? latestVersion = null;
        var appliedCount = 0;
        var deferredCount = 0;
        var versionBlocked = false;

        foreach (var patch in patchesToApply)
        {
            var patchName = patch.Name;

            try
            {
                logger.ApplyingPatch(patchName, patch.Version, _kernelVersion);
                await patch.Up();

                // A patch that left quarantined event stores out has not reached them. It is not recorded as applied,
                // and the system version stays below it, so it runs again on a later startup. Patches after it are
                // recorded by name, so they are not repeated.
                if (IsDeferred(patch))
                {
                    versionBlocked = true;
                    deferredCount++;
                    continue;
                }

                await RecordApplied(patch);

                latestVersion = VersionAfter(latestVersion, patch, versionBlocked);
                appliedCount++;
                logger.PatchAppliedSuccessfully(patchName);
            }
            catch (Exception ex)
            {
                logger.PatchApplicationFailed(patchName, ex);
                throw;
            }
        }

        if (latestVersion is not null)
        {
            await system.SetVersion(latestVersion);
            logger.UpdatedSystemVersion(latestVersion);
        }

        logger.PatchApplicationCompleted(appliedCount, skippedCount + deferredCount);
    }

    async Task<HashSet<string>> AppliedPatchNames(IEnumerable<ICanApplyPatch> candidatePatches)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var patch in candidatePatches)
        {
            if (await storage.System.Patches.Has(patch.Name))
            {
                names.Add(patch.Name);
            }
        }

        return names;
    }

    bool IsDeferred(ICanApplyPatch patch)
    {
        if (patch is not IReportSkippedEventStores { SkippedEventStores.Count: > 0 } skipped)
        {
            return false;
        }

        logger.PatchDeferred(patch.Name, string.Join(", ", skipped.SkippedEventStores.Select(_ => ResourceNameText.Escape(_.Value))));
        return true;
    }

    async Task RecordApplied(ICanApplyPatch patch)
    {
        var patchRecord = new Patch(patch.Name, patch.Version, DateTimeOffset.UtcNow);
        await storage.System.Patches.Save(patchRecord);

        State = State with
        {
            AppliedPatches = State.AppliedPatches.Append(patchRecord)
        };

        await WriteStateAsync();
    }
}
