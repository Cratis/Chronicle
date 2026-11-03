// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManager.when_applying_patches;

public class and_the_caller_named_its_silo : given.a_patch_manager
{
    object? _seenByTheWorker;
    SiloAddress _callerSilo;

    async Task Establish()
    {
        _callerSilo = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11111), 1);
        _worker.ApplyPatchesFor(Arg.Any<Cratis.Choreography.Patching.SemanticVersion>()).Returns(_ =>
        {
            _seenByTheWorker = RequestContext.Get(PatchManager.ChoreographyCallerSiloKey);
            return Cratis.Choreography.Patching.PatchOutcome.Completed;
        });
        await CreateManager();
    }

    Task Because()
    {
        RequestContext.Set(PatchManagerRelocation.CallerSiloKey, _callerSilo);
        return _manager.ApplyPatchesFor(_caller);
    }

    void Destroy()
    {
        RequestContext.Remove(PatchManagerRelocation.CallerSiloKey);
        RequestContext.Remove(PatchManager.ChoreographyCallerSiloKey);
    }

    [Fact] void should_tell_the_worker_where_the_caller_is() => _seenByTheWorker.ShouldEqual(_callerSilo);
}
