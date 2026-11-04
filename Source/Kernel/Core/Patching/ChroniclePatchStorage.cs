// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Keeps the record of applied patches in Chronicle's system storage, where every release has always kept it, so a store that was patched
/// by an earlier release is not patched again.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> holding the system storage.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the grain that holds the version patching has reached.</param>
public class ChroniclePatchStorage(IStorage storage, IGrainFactory grainFactory) : IPatchStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<AppliedPatch>> GetAll() =>
        (await storage.System.Patches.GetAll()).Select(patch => new AppliedPatch(patch.Name, patch.Version.ToPatching(), patch.AppliedAt)).ToArray();

    /// <inheritdoc/>
    public Task<bool> Has(string patchName) => storage.System.Patches.Has(patchName);

    /// <inheritdoc/>
    public Task Save(AppliedPatch patch) =>
        storage.System.Patches.Save(new Concepts.Patching.Patch(patch.Name, patch.Version.ToChronicle(), patch.AppliedAt));

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetPatchedVersion() => (await grainFactory.GetSystem().GetVersion())?.ToPatching();

    /// <inheritdoc/>
    public Task SetPatchedVersion(SemanticVersion version) => grainFactory.GetSystem().SetVersion(version.ToChronicle());

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetHostVersion() => (await storage.System.GetPatchingKernelVersion())?.ToPatching();

    /// <inheritdoc/>
    public Task SetHostVersion(SemanticVersion version) => storage.System.SetPatchingKernelVersion(version.ToChronicle());
}
