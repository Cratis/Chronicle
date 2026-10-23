// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Storage.Sql.for_SystemStorage;

public class when_no_kernel_version_has_been_recorded : given.a_system_storage
{
    SemanticVersion? _version;

    async Task Because() => _version = await _storage.GetPatchingKernelVersion();

    [Fact] void should_return_no_version() => _version.ShouldBeNull();
}
