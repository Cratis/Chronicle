// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Storage.Sql.for_SystemStorage.when_setting_patching_kernel_version;

public class and_system_information_exists : given.a_system_storage
{
    SystemInformation? _systemInformation;
    SemanticVersion? _kernelVersion;

    async Task Establish() => await _storage.SetSystemInformation(new SystemInformation(new SemanticVersion(19, 90, 0)));

    async Task Because()
    {
        await _storage.SetPatchingKernelVersion(new SemanticVersion(19, 99, 0));
        await _storage.SetPatchingKernelVersion(new SemanticVersion(19, 100, 0));
        _systemInformation = await _storage.GetSystemInformation();
        _kernelVersion = await _storage.GetPatchingKernelVersion();
    }

    [Fact] void should_preserve_the_system_version() => _systemInformation!.Version.ShouldEqual(new SemanticVersion(19, 90, 0));
    [Fact] void should_update_the_patching_kernel_version() => _kernelVersion.ShouldEqual(new SemanticVersion(19, 100, 0));
}
