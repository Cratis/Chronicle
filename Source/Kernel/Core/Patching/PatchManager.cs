// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Sys;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchManager"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="patches">All available <see cref="ICanApplyPatch"/> instances.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.PatchManager)]
public class PatchManager(
    IGrainFactory grainFactory,
    IStorage storage,
    IInstancesOf<ICanApplyPatch> patches,
    ILogger<PatchManager> logger) : Grain<PatchManagerState>, IPatchManager
{
    /// <inheritdoc/>
    public async Task ApplyPatches()
    {
        System.IO.File.AppendAllText("/tmp/diag-patchmgr.log", $"[DIAG-PM] ApplyPatches called at {DateTimeOffset.UtcNow:O}\n{Environment.StackTrace}\n\n");
        logger.StartingPatchApplication();

        var system = grainFactory.GetSystem();
        var currentVersion = await system.GetVersion() ?? SemanticVersion.NotSet;
        logger.CurrentSystemVersion(currentVersion);

        var allPatches = patches.ToList();
        var patchesToApply = allPatches
            .Where(p => p.Version > currentVersion)
            .OrderBy(p => p.Version)
            .ToList();

        if (patchesToApply.Count == 0)
        {
            logger.NoPatchesToApply();
            return;
        }

        logger.FoundPatchesToApply(patchesToApply.Count);

        SemanticVersion? latestVersion = null;

        foreach (var patch in patchesToApply)
        {
            var patchName = patch.Name;

            if (await storage.System.Patches.Has(patchName))
            {
                logger.PatchAlreadyApplied(patchName);
                continue;
            }

            try
            {
                logger.ApplyingPatch(patchName, patch.Version);
                await patch.Up();

                var patchRecord = new Patch(patchName, patch.Version, DateTimeOffset.UtcNow);
                await storage.System.Patches.Save(patchRecord);

                State = State with
                {
                    AppliedPatches = State.AppliedPatches.Append(patchRecord)
                };

                await WriteStateAsync();

                latestVersion = patch.Version;
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

        logger.PatchApplicationCompleted();
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        System.IO.File.AppendAllText("/tmp/diag-patchmgr.log", $"[DIAG-PM] OnActivateAsync called at {DateTimeOffset.UtcNow:O}\n");
        if (State is null || State == PatchManagerState.Empty)
        {
            var appliedPatches = await storage.System.Patches.GetAll();
            State = new PatchManagerState(appliedPatches);
            await WriteStateAsync();
        }
        System.IO.File.AppendAllText("/tmp/diag-patchmgr.log", $"[DIAG-PM] OnActivateAsync completed\n");
        await base.OnActivateAsync(cancellationToken);
    }
}
