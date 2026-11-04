// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Sys;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Keeps the record of applied patches in Chronicle's system storage, where every release has always kept it, so a store that was patched
/// by an earlier release is not patched again.
/// </summary>
/// <remarks>
/// A patch that left quarantined event stores or namespaces out (see <see cref="IReportSkippedResources"/>) has not reached them. It is not
/// recorded as applied, and the patched version is never moved past it, so it runs again on a later startup once the quarantine is
/// resolved. Patches after it are recorded by name, so they are not repeated.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> holding the system storage.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the grain that holds the version patching has reached.</param>
/// <param name="patches">Optional <see cref="IInstancesOf{T}"/> the available patches, for telling which have skipped resources; without it every patch is recorded.</param>
/// <param name="logger">Optional <see cref="ILogger"/> for logging.</param>
public class ChroniclePatchStorage(
    IStorage storage,
    IGrainFactory grainFactory,
    IInstancesOf<ICanApplyPatch>? patches = null,
    ILogger<ChroniclePatchStorage>? logger = null) : IPatchStorage
{
    readonly ILogger<ChroniclePatchStorage> _logger = logger ?? NullLogger<ChroniclePatchStorage>.Instance;

    /// <inheritdoc/>
    public async Task<IEnumerable<AppliedPatch>> GetAll() =>
        (await storage.System.Patches.GetAll()).Select(patch => new AppliedPatch(patch.Name, patch.Version.ToPatching(), patch.AppliedAt)).ToArray();

    /// <inheritdoc/>
    public Task<bool> Has(string patchName) => storage.System.Patches.Has(patchName);

    /// <inheritdoc/>
    public Task Save(AppliedPatch patch)
    {
        if (IsDeferred(patch.Name))
        {
            return Task.CompletedTask;
        }

        return storage.System.Patches.Save(new Concepts.Patching.Patch(patch.Name, patch.Version.ToChronicle(), patch.AppliedAt));
    }

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetPatchedVersion() => (await grainFactory.GetSystem().GetVersion())?.ToPatching();

    /// <inheritdoc/>
    public async Task SetPatchedVersion(SemanticVersion version)
    {
        var reachable = await HighestContiguousCompletedVersion(version);
        if (reachable is not null)
        {
            await grainFactory.GetSystem().SetVersion(reachable.ToChronicle());
        }
    }

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetHostVersion() => (await storage.System.GetPatchingKernelVersion())?.ToPatching();

    /// <inheritdoc/>
    public Task SetHostVersion(SemanticVersion version) => storage.System.SetPatchingKernelVersion(version.ToChronicle());

    /// <summary>
    /// Gets the highest version the system can be moved to: the version of the last patch in the unbroken run of
    /// completed patches that starts at the oldest patch not yet passed. The version never moves past a patch that is not
    /// completed, whatever applies after it - and moves on again once that patch completes, even when later patches
    /// were recorded in the meantime.
    /// </summary>
    /// <param name="requested">The <see cref="SemanticVersion"/> that was asked for.</param>
    /// <returns>The <see cref="SemanticVersion"/> to move to, or null when no patch is completed yet.</returns>
    async Task<SemanticVersion?> HighestContiguousCompletedVersion(SemanticVersion requested)
    {
        if (patches is null)
        {
            return requested;
        }

        var current = await GetPatchedVersion() ?? SemanticVersion.NotSet;
        SemanticVersion? version = null;
        foreach (var patch in patches.Where(_ => _.Version > current).OrderBy(_ => _.Version))
        {
            if (!await storage.System.Patches.Has(patch.Name))
            {
                break;
            }

            version = patch.Version;
        }

        return version;
    }

    bool IsDeferred(string patchName)
    {
        var patch = patches?.FirstOrDefault(_ => _.Name == patchName);
        if (patch is not IReportSkippedResources { SkippedResources.Any: true } skipped)
        {
            return false;
        }

        _logger.PatchDeferred(patchName, skipped.SkippedResources.Describe());
        return true;
    }
}
