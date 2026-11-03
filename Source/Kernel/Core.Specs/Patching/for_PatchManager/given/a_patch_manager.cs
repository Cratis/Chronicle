// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Orleans.TestKit;
using ChoreographyPatchManager = Cratis.Choreography.Patching.IPatchManager;

namespace Cratis.Chronicle.Patching.for_PatchManager.given;

public class a_patch_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected ChoreographyPatchManager _worker;
    protected PatchManager _manager;
    protected SemanticVersion _caller = new(19, 100, 0, "rc.1", "abc");

    void Establish()
    {
        _worker = Substitute.For<ChoreographyPatchManager>();
        _silo.AddProbe(_ => _worker);
    }

    protected async Task CreateManager() => _manager = await _silo.CreateGrainAsync<PatchManager>(0);
}
