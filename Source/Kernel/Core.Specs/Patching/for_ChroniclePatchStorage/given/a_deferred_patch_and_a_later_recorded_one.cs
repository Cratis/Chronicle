// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.EventStores;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

/// <summary>
/// Two patches: an older one that leaves a resource out until told otherwise, and a later one that is recorded straight away.
/// </summary>
public class a_deferred_patch_and_a_later_recorded_one : a_patching_run
{
    protected static readonly SemanticVersion LaterVersion = new(19, 112, 0);

    protected DeferrablePatch _deferred;
    protected ICanApplyPatch _later;

    void Establish()
    {
        _deferred = new DeferrablePatch();
        _later = Substitute.For<ICanApplyPatch>();
        _later.Name.Returns("later-patch");
        _later.Version.Returns(LaterVersion);

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
    }
}
