// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_bounding_by_a_ceiling;

public class and_the_ceiling_is_a_stable_release : Specification
{
    [Fact] void should_include_an_earlier_release() => ReleaseVersion.IsAtOrBefore("19.4.0", "19.4.9").ShouldBeTrue();
    [Fact] void should_include_the_ceiling_itself() => ReleaseVersion.IsAtOrBefore("19.4.9", "19.4.9").ShouldBeTrue();
    [Fact] void should_exclude_a_later_minor() => ReleaseVersion.IsAtOrBefore("19.6.1", "19.4.9").ShouldBeFalse();
    [Fact] void should_order_minors_numerically() => ReleaseVersion.IsAtOrBefore("19.10.0", "19.9.0").ShouldBeFalse();
}
