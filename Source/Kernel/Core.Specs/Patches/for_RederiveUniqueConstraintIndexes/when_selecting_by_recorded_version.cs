// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Patches;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes;

/// <summary>
/// A patch is selected only when its version is above the recorded system version. The version is a placeholder that is set when the release train is cut.
/// </summary>
public class when_selecting_by_recorded_version : given.a_rederive_patch
{
    static bool IsSelected(SemanticVersion patch, SemanticVersion recorded) => patch > recorded;

    [Fact] void should_be_above_the_previous_patch_version() => (_patch.Version > new SemanticVersion(20, 1, 0)).ShouldBeTrue();
    [Fact] void should_use_the_single_version_constant() => _patch.Version.ShouldEqual(RederiveUniqueConstraintIndexes.PatchVersion);
    [Fact] void should_run_when_the_recorded_version_is_lower() => IsSelected(_patch.Version, new SemanticVersion(20, 1, 0)).ShouldBeTrue();
    [Fact] void should_not_run_when_the_recorded_version_is_equal() => IsSelected(_patch.Version, _patch.Version).ShouldBeFalse();
    [Fact] void should_not_run_when_the_recorded_version_is_higher() => IsSelected(_patch.Version, new SemanticVersion(20, 2, 0)).ShouldBeFalse();
}
