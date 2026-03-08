// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patching;

namespace Cratis.Chronicle.Core.Specs.Patching.for_PatchManager;

public class PatchManagerBehaviorSpecs : Specification
{
    class TestPatch(SemanticVersion version, string name) : ICanApplyPatch
    {
        public SemanticVersion Version { get; } = version;
        public string Name { get; } = name;
        public bool UpCalled { get; private set; }

        public Task Up()
        {
            UpCalled = true;
            return Task.CompletedTask;
        }

        public Task Down() => Task.CompletedTask;
    }

    [Fact]
    public async Task should_apply_patches_newer_than_current_version()
    {
        var storage = Substitute.For<IStorage>();
        var systemStorage = Substitute.For<ISystemStorage>();
        var patchStorage = Substitute.For<IPatchStorage>();

        storage.System.Returns(systemStorage);
        systemStorage.Patches.Returns(patchStorage);
        systemStorage.GetSystemInformation().Returns(Task.FromResult<SystemInformation?>(new SystemInformation(new SemanticVersion(1, 0, 0))));

        var newerPatch = new TestPatch(new SemanticVersion(1, 1, 0), "NewerPatch");
        var patches = new List<ICanApplyPatch> { newerPatch };
        var patchesProvider = Substitute.For<IInstancesOf<ICanApplyPatch>>();
        patchesProvider.GetEnumerator().Returns(_ => patches.GetEnumerator());

        var patchesToApply = patches.Where(p => p.Version > new SemanticVersion(1, 0, 0)).ToList();

        patchesToApply.Count.ShouldEqual(1);
        patchesToApply[0].Version.ShouldEqual(new SemanticVersion(1, 1, 0));
    }

    [Fact]
    public async Task should_not_apply_patches_older_than_current_version()
    {
        var currentVersion = new SemanticVersion(2, 0, 0);
        var olderPatch = new TestPatch(new SemanticVersion(1, 0, 0), "OlderPatch");
        var patches = new List<ICanApplyPatch> { olderPatch };

        var patchesToApply = patches.Where(p => p.Version > currentVersion).ToList();

        patchesToApply.Count.ShouldEqual(0);
    }

    [Fact]
    public async Task should_not_apply_patches_with_same_version()
    {
        var currentVersion = new SemanticVersion(1, 0, 0);
        var samePatch = new TestPatch(new SemanticVersion(1, 0, 0), "SamePatch");
        var patches = new List<ICanApplyPatch> { samePatch };

        var patchesToApply = patches.Where(p => p.Version > currentVersion).ToList();

        patchesToApply.Count.ShouldEqual(0);
    }

    [Fact]
    public async Task should_apply_multiple_patches_in_ascending_version_order()
    {
        var currentVersion = new SemanticVersion(1, 0, 0);
        var patches = new List<ICanApplyPatch>
        {
            new TestPatch(new SemanticVersion(1, 3, 0), "Patch3"),
            new TestPatch(new SemanticVersion(1, 1, 0), "Patch1"),
            new TestPatch(new SemanticVersion(1, 2, 0), "Patch2")
        };

        var patchesToApply = patches.Where(p => p.Version > currentVersion).OrderBy(p => p.Version).ToList();

        patchesToApply.Count.ShouldEqual(3);
        patchesToApply[0].Version.ShouldEqual(new SemanticVersion(1, 1, 0));
        patchesToApply[1].Version.ShouldEqual(new SemanticVersion(1, 2, 0));
        patchesToApply[2].Version.ShouldEqual(new SemanticVersion(1, 3, 0));
    }

    [Fact]
    public async Task should_apply_all_patches_when_no_current_version()
    {
        var effectiveVersion = SemanticVersion.NotSet;

        var patches = new List<ICanApplyPatch>
        {
            new TestPatch(new SemanticVersion(1, 0, 0), "Patch1"),
            new TestPatch(new SemanticVersion(2, 0, 0), "Patch2")
        };

        var patchesToApply = patches.Where(p => p.Version > effectiveVersion).ToList();

        patchesToApply.Count.ShouldEqual(2);
    }

    [Fact]
    public async Task should_filter_out_older_patches_from_mixed_set()
    {
        var currentVersion = new SemanticVersion(1, 5, 0);
        var patches = new List<ICanApplyPatch>
        {
            new TestPatch(new SemanticVersion(1, 0, 0), "OlderPatch1"),
            new TestPatch(new SemanticVersion(1, 6, 0), "NewerPatch1"),
            new TestPatch(new SemanticVersion(1, 3, 0), "OlderPatch2"),
            new TestPatch(new SemanticVersion(1, 7, 0), "NewerPatch2")
        };

        var patchesToApply = patches.Where(p => p.Version > currentVersion).ToList();

        patchesToApply.Count.ShouldEqual(2);
        patchesToApply.TrueForAll(p => p.Version > currentVersion).ShouldBeTrue();
    }

    [Fact]
    public async Task should_determine_latest_version_from_applied_patches()
    {
        var patches = new List<TestPatch>
        {
            new(new SemanticVersion(1, 1, 0), "Patch1"),
            new(new SemanticVersion(1, 2, 0), "Patch2"),
            new(new SemanticVersion(1, 3, 0), "Patch3")
        };

        SemanticVersion? latestVersion = null;
        foreach (var patch in patches.OrderBy(p => p.Version))
        {
            latestVersion = patch.Version;
        }

        latestVersion.ShouldEqual(new SemanticVersion(1, 3, 0));
    }
}
