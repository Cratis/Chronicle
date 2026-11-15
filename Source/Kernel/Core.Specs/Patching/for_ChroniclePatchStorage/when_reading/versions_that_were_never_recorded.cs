// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.when_reading;

public class versions_that_were_never_recorded : given.a_patch_storage
{
    SemanticVersion? _patched;
    SemanticVersion? _host;

    void Establish()
    {
        _system.GetVersion().Returns(Task.FromResult<ChronicleVersion?>(null));
        _systemStorage.GetPatchingKernelVersion().Returns(Task.FromResult<ChronicleVersion?>(null));
    }

    async Task Because()
    {
        _patched = await _storage.GetPatchedVersion();
        _host = await _storage.GetHostVersion();
    }

    [Fact] void should_not_have_a_patched_version() => _patched.ShouldBeNull();
    [Fact] void should_not_have_a_host_version() => _host.ShouldBeNull();
}
