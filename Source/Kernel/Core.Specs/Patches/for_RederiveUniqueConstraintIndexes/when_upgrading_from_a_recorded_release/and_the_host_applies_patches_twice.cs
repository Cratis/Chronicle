// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Patching;
using Cratis.Chronicle.Patches;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestKit;
using ChoreographyPatchManager = Cratis.Choreography.Patching.PatchManager;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes.when_upgrading_from_a_recorded_release;

/// <summary>
/// An upgrade from a store whose last host was 19.130.0, with the 20.1.0 repeat move patch as the last patch that ran, to the release that carries this patch.
/// </summary>
public class and_the_host_applies_patches_twice : given.a_rederive_patch
{
    static readonly SemanticVersion _recordedHost = new(19, 130, 0);
    static readonly SemanticVersion _upgradedHost = new(19, 131, 0);
    static readonly SemanticVersion _repeatMoveVersion = new(20, 1, 0);

    TestKitSilo _silo;
    InMemoryPatchStorage _patchStorage;
    PatchOutcome _first;
    PatchOutcome _second;
    SemanticVersion? _recordedVersion;
    int _startsAfterFirst;
    int _startsAfterSecond;

    async Task Establish()
    {
        _otherStorage.CountPendingSpellings().Returns(Task.FromResult(2));
        _patchStorage = new();
        await _patchStorage.SetPatchedVersion(_repeatMoveVersion);
        await _patchStorage.SetHostVersion(_recordedHost);

        var patches = Substitute.For<IInstancesOf<ICanApplyPatch>>();
        patches.GetEnumerator().Returns(_ => new List<ICanApplyPatch> { _patch }.GetEnumerator());

        _silo = new();
        _silo.ServiceProvider.AddService<IPatchStorage>(_patchStorage);
        _silo.ServiceProvider.AddService(patches);
        _silo.ServiceProvider.AddService(Options.Create(new PatchingOptions { HostVersion = _upgradedHost }));
        _silo.ServiceProvider.AddService(Substitute.For<ILogger<ChoreographyPatchManager>>());
    }

    async Task Because()
    {
        var manager = await _silo.CreateGrainAsync<ChoreographyPatchManager>(0);
        _first = await manager.ApplyPatchesFor(_upgradedHost);
        _startsAfterFirst = _jobsManager.ReceivedCalls().Count();
        _second = await manager.ApplyPatchesFor(_upgradedHost);
        _startsAfterSecond = _jobsManager.ReceivedCalls().Count();
        _recordedVersion = await _patchStorage.GetPatchedVersion();
    }

    [Fact] void should_complete_both_times() => (_first == PatchOutcome.Completed && _second == PatchOutcome.Completed).ShouldBeTrue();
    [Fact] void should_run_the_patch_on_the_first_start() => _startsAfterFirst.ShouldBeGreaterThan(0);
    [Fact] void should_not_run_the_patch_again() => _startsAfterSecond.ShouldEqual(_startsAfterFirst);
    [Fact] void should_record_the_patch_version() => _recordedVersion.ShouldEqual(RederiveUniqueConstraintIndexes.PatchVersion);
    [Fact] void should_be_above_the_repeat_move_version() => (RederiveUniqueConstraintIndexes.PatchVersion > _repeatMoveVersion).ShouldBeTrue();
}
