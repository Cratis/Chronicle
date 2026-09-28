// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_bound_by_a_ceiling;

/// <summary>
/// The ceiling decides which releases the one being cut still has to serve, so ordering them as text - where
/// 16.9.0 sorts after 16.36.0 - would silently include or exclude the wrong ones.
/// </summary>
public class and_the_ceiling_is_a_stable_release : Specification
{
    [Fact] void should_include_the_ceiling_itself() => ReleaseVersion.IsAtOrBefore("16.36.0", "16.36.0").ShouldBeTrue();
    [Fact] void should_include_an_earlier_patch_of_the_ceiling() => ReleaseVersion.IsAtOrBefore("16.36.1", "16.36.2").ShouldBeTrue();
    [Fact] void should_exclude_a_later_minor() => ReleaseVersion.IsAtOrBefore("16.37.0", "16.36.2").ShouldBeFalse();
    [Fact] void should_order_by_minor_numerically() => ReleaseVersion.IsAtOrBefore("16.10.0", "16.9.0").ShouldBeFalse();
    [Fact] void should_treat_a_missing_component_as_zero() => ReleaseVersion.IsAtOrBefore("16.36", "16.36.0").ShouldBeTrue();
}
