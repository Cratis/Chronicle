// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_patch_manager_is_asked_to_patch : given.a_startup_task
{
    object? _caller;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ =>
    {
        _caller = RequestContext.Get(PatchManagerRelocation.CallerSiloKey);
        return Task.FromResult(PatchOutcome.Completed);
    });

    Task Because() => Execute();

    [Fact] void should_name_the_callers_silo() => _caller.ShouldEqual(_localSiloDetails.SiloAddress);
    [Fact] void should_remove_the_hint_after_startup() => RequestContext.Get(PatchManagerRelocation.CallerSiloKey).ShouldBeNull();
}
