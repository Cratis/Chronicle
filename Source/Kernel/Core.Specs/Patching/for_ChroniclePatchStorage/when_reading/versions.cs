// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.when_reading;

public class versions : given.a_patch_storage
{
    SemanticVersion? _patched;
    SemanticVersion? _host;

    void Establish()
    {
        _system.GetVersion().Returns(Task.FromResult<ChronicleVersion?>(new ChronicleVersion(19, 112, 0)));
        _systemStorage.GetPatchingKernelVersion().Returns(Task.FromResult<ChronicleVersion?>(new ChronicleVersion(19, 118, 0)));
    }

    async Task Because()
    {
        _patched = await _storage.GetPatchedVersion();
        _host = await _storage.GetHostVersion();
    }

    [Fact] void should_read_the_patched_version_from_the_system_grain() => _patched.ShouldEqual(new SemanticVersion(19, 112, 0));
    [Fact] void should_read_the_host_version_from_the_patching_kernel_version() => _host.ShouldEqual(new SemanticVersion(19, 118, 0));
}
