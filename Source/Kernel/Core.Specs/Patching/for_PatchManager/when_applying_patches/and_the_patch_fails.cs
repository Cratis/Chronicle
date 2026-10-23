// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_patch_fails : given.a_patch_manager
{
    Exception _error;

    void Establish() => _patch.Up().Returns(Task.FromException(new InvalidOperationException("patch failed")));

    async Task Because()
    {
        await CreateManager();
        _error = await Catch.Exception(_manager.ApplyPatches);
    }

    [Fact] void should_fail_the_call() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_not_record_a_kernel_version() => await _systemStorage.DidNotReceive().SetPatchingKernelVersion(Arg.Any<SemanticVersion>());
}
