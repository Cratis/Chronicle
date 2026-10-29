// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_another_grain_has_no_compatible_silo : given.a_startup_task
{
    readonly OrleansException _original = new("No active nodes are compatible with grain observer and interface Cratis.Chronicle.Observation.IObserver version 3.");
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatchesFor(Arg.Any<SemanticVersion>()).Returns(_ => Task.FromException<PatchOutcome>(_original));
    async Task Because() => _failure = await Catch.Exception(Execute);

    [Fact] void should_not_retry_another_interface() => _failure.ShouldEqual(_original);
    [Fact] async Task should_call_only_once() => await _patchManager.Received(1).ApplyPatchesFor(Arg.Any<SemanticVersion>());
}
