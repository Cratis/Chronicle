// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.EventStores;

namespace Cratis.Chronicle.Patching.for_PatchManager.given;

/// <summary>
/// Two patches: an older one that leaves a resource out until told otherwise, and a later one that is recorded
/// straight away. Records saved by the manager are answered by the storage, as the real one would.
/// </summary>
public class a_deferred_patch_and_a_later_recorded_one : a_patch_manager
{
    protected static readonly SemanticVersion LaterVersion = new(19, 120, 0);

    protected DeferrablePatch _deferred;
    protected ICanApplyPatch _later;

    void Establish()
    {
        _deferred = new DeferrablePatch();
        _later = Substitute.For<ICanApplyPatch>();
        _later.Name.Returns("later-patch");
        _later.Version.Returns(LaterVersion);

        var recorded = new HashSet<string>();
        _systemStorage.Patches.Has(Arg.Any<string>()).Returns(_ => Task.FromResult(recorded.Contains(_.Arg<string>())));
        _systemStorage.Patches.Save(Arg.Any<Patch>()).Returns(_ =>
        {
            recorded.Add(_.Arg<Patch>().Name);
            return Task.CompletedTask;
        });

        _patches.Clear();
        _patches.Add(_deferred);
        _patches.Add(_later);
    }

    protected class DeferrablePatch : ICanApplyPatch, IReportSkippedResources
    {
        public bool Skips { get; set; } = true;

        public SkippedResources SkippedResources { get; private set; } = SkippedResources.None;

        public string Name => "deferred-patch";

        public SemanticVersion Version => new(19, 110, 0);

        public Task Up()
        {
            SkippedResources = Skips ? new SkippedResources([], [("some-store", "some-namespace")]) : SkippedResources.None;
            return Task.CompletedTask;
        }

        public Task Down() => Task.CompletedTask;
    }
}
