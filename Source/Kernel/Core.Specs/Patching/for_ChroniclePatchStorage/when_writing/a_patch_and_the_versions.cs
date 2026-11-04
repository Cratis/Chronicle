// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using ChroniclePatch = Cratis.Chronicle.Concepts.Patching.Patch;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.when_writing;

public class a_patch_and_the_versions : given.a_patch_storage
{
    static readonly DateTimeOffset _appliedAt = new(2026, 5, 1, 8, 30, 0, TimeSpan.Zero);

    async Task Because()
    {
        await _storage.Save(new AppliedPatch("rename-reactors", new SemanticVersion(15, 2, 4), _appliedAt));
        await _storage.SetPatchedVersion(new SemanticVersion(15, 2, 4));
        await _storage.SetHostVersion(new SemanticVersion(19, 118, 0));
    }

    [Fact] async Task should_save_the_patch_where_earlier_releases_look() => await _patches.Received(1).Save(new ChroniclePatch("rename-reactors", new ChronicleVersion(15, 2, 4), _appliedAt));
    [Fact] async Task should_keep_the_patched_version_on_the_system_grain() => await _system.Received(1).SetVersion(new ChronicleVersion(15, 2, 4));
    [Fact] async Task should_keep_the_host_version_as_the_patching_kernel_version() => await _systemStorage.Received(1).SetPatchingKernelVersion(new ChronicleVersion(19, 118, 0));
}
