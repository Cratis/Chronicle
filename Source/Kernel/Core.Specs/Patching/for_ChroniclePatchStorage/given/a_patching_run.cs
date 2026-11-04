// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Sys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ChoreographyPatchManager = Cratis.Choreography.Patching.PatchManager;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

/// <summary>
/// Runs the patch manager of Cratis Choreography against <see cref="ChroniclePatchStorage"/>, with the records of applied patches and
/// the patched version kept the way the real storage keeps them.
/// </summary>
public class a_patching_run : Specification
{
    protected IStorage _storage;
    protected ISystemStorage _systemStorage;
    protected ISystem _system;
    protected IGrainFactory _grainFactory;
    protected List<ICanApplyPatch> _patches = [];
    protected ChronicleVersion? _patchedVersion;
    protected ChoreographyPatchManager _manager;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _systemStorage = Substitute.For<ISystemStorage>();
        _storage.System.Returns(_systemStorage);

        var recorded = new HashSet<string>();
        var records = Substitute.For<IPatchStorage>();
        records.Has(Arg.Any<string>()).Returns(_ => Task.FromResult(recorded.Contains(_.Arg<string>())));
        records.Save(Arg.Any<Concepts.Patching.Patch>()).Returns(_ =>
        {
            recorded.Add(_.Arg<Concepts.Patching.Patch>().Name);
            return Task.CompletedTask;
        });
        _systemStorage.Patches.Returns(records);

        _system = Substitute.For<ISystem>();
        _system.GetVersion().Returns(_ => Task.FromResult(_patchedVersion));
        _system.SetVersion(Arg.Any<ChronicleVersion>()).Returns(_ =>
        {
            _patchedVersion = _.Arg<ChronicleVersion>();
            return Task.CompletedTask;
        });
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<ISystem>(0).Returns(_system);

        var patches = Substitute.For<IInstancesOf<ICanApplyPatch>>();
        patches.GetEnumerator().Returns(_ => _patches.GetEnumerator());

        var hostVersion = new SemanticVersion(19, 118, 0);
        _manager = new ChoreographyPatchManager(
            new ChroniclePatchStorage(_storage, _grainFactory, patches),
            patches,
            Options.Create(new PatchingOptions { HostVersion = hostVersion }),
            NullLogger<ChoreographyPatchManager>.Instance);
    }

    protected Task ApplyPatches() => _manager.ApplyPatchesFor(new SemanticVersion(19, 118, 0));
}
