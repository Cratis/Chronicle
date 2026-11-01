// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_manifest_is_delayed : given.a_startup_task
{
    int _calls;
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => ++_calls < 3
        ? Task.FromException<PatchOutcome>(new OrleansException(ChronicleServerStartupTask.PatchManagerPlacementFailure + "2."))
        : Task.FromResult(PatchOutcome.Completed));

    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_finish_startup() => _failure.ShouldBeNull();
    [Fact] void should_retry_until_the_manifest_arrives() => _calls.ShouldEqual(3);
}
