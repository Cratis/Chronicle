// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_comparing;

public class and_a_version_has_a_pre_release_suffix : Specification
{
    [Fact] void should_compare_the_same_numeric_core_as_equal() => ReleaseVersion.Compare("19.4.9-hotfix.1", "19.4.9").ShouldEqual(0);
    [Fact] void should_keep_the_patch_before_the_suffix() => ReleaseVersion.Compare("19.4.9-hotfix.1", "19.4.8").ShouldBeGreaterThan(0);
    [Fact] void should_ignore_build_metadata() => ReleaseVersion.Compare("19.4.9+build.5", "19.4.9").ShouldEqual(0);
}
