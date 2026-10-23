// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Sys;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patching.for_PatchManager.given;

public class a_patch_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected ISystemStorage _systemStorage;
    protected ISystem _system;
    protected ICanApplyPatch _patch;
    protected PatchManager _manager;
    protected SemanticVersion _version = new(19, 100, 0);
    protected List<ICanApplyPatch> _patches = [];

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        _systemStorage = Substitute.For<ISystemStorage>();
        storage.System.Returns(_systemStorage);
        _system = Substitute.For<ISystem>();
        _silo.AddProbe<ISystem>(_ => _system);
        _silo.AddService(storage);
        _silo.AddService(NullLogger<PatchManager>.Instance);
        _patch = Substitute.For<ICanApplyPatch>();
        _patch.Name.Returns("a-patch");
        _patch.Version.Returns(new SemanticVersion(19, 90, 0));
        _patches.Add(_patch);
        var patches = Substitute.For<IInstancesOf<ICanApplyPatch>>();
        patches.GetEnumerator().Returns(_ => _patches.GetEnumerator());
        _silo.AddService(patches);
    }

    protected async Task CreateManager()
    {
        _silo.AddService(_version);
        _silo.StorageManager.GetStorage<PatchManagerState>(typeof(PatchManager).FullName!).State = PatchManagerState.Empty;
        _manager = await _silo.CreateGrainAsync<PatchManager>(0);
    }
}
