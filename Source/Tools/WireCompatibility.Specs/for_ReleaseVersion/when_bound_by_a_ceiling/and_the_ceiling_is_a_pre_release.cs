// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_bound_by_a_ceiling;

/// <summary>
/// The ceiling is the version being released, and a release can be a pre-release (19.4.9-hotfix.1). Its suffix
/// reads as a zero numeric component - 19.4.9-hotfix.1 would order before 19.4.8 it follows - so it has to be
/// dropped before comparing, or the hotfix excludes the very line it is cut from.
/// </summary>
public class and_the_ceiling_is_a_pre_release : Specification
{
    [Fact] void should_include_a_release_the_pre_release_follows() => ReleaseVersion.IsAtOrBefore("19.4.8", "19.4.9-hotfix.1").ShouldBeTrue();
    [Fact] void should_include_a_release_sharing_the_numeric_core() => ReleaseVersion.IsAtOrBefore("19.4.9", "19.4.9-hotfix.1").ShouldBeTrue();
    [Fact] void should_exclude_a_release_the_pre_release_cannot_serve() => ReleaseVersion.IsAtOrBefore("19.6.1", "19.4.9-hotfix.1").ShouldBeFalse();
    [Fact] void should_strip_the_suffix() => ReleaseVersion.WithoutPreRelease("19.4.9-hotfix.1").ShouldEqual("19.4.9");
    [Fact] void should_keep_a_stable_version_whole() => ReleaseVersion.WithoutPreRelease("19.4.9").ShouldEqual("19.4.9");
}
