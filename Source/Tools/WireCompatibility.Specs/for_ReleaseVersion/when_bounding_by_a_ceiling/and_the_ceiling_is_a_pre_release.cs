// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_bounding_by_a_ceiling;

public class and_the_ceiling_is_a_pre_release : Specification
{
    [Fact] void should_include_a_stable_release_with_the_same_core() => ReleaseVersion.IsAtOrBefore("19.4.9", "19.4.9-hotfix.1").ShouldBeTrue();
    [Fact] void should_include_an_earlier_patch() => ReleaseVersion.IsAtOrBefore("19.4.0", "19.4.9-hotfix.1").ShouldBeTrue();
    [Fact] void should_exclude_a_later_minor() => ReleaseVersion.IsAtOrBefore("19.5.0", "19.4.9-hotfix.1").ShouldBeFalse();
}
