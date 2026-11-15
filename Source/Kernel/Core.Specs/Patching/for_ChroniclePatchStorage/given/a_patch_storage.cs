// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

public class a_patch_storage : Specification
{
    protected ISystemStorage _systemStorage;
    protected IPatchStorage _patches;
    protected ISystem _system;
    protected IGrainFactory _grainFactory;
    protected ChroniclePatchStorage _storage;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        _systemStorage = Substitute.For<ISystemStorage>();
        _patches = Substitute.For<IPatchStorage>();
        _systemStorage.Patches.Returns(_patches);
        storage.System.Returns(_systemStorage);
        _system = Substitute.For<ISystem>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<ISystem>(0).Returns(_system);
        _storage = new ChroniclePatchStorage(storage, _grainFactory);
    }
}
