// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_comparing;

/// <summary>
/// The floor decides which releases the gate still measures against, so ordering them as text - where 16.9.0
/// sorts after 16.36.0 - would silently include or exclude the wrong ones.
/// </summary>
public class and_the_versions_differ : Specification
{
    [Fact] void should_order_by_minor_numerically() => ReleaseVersion.IsAtOrAfter("16.36.0", "16.9.0").ShouldBeTrue();
    [Fact] void should_exclude_an_earlier_minor() => ReleaseVersion.IsAtOrAfter("16.35.2", "16.36.0").ShouldBeFalse();
    [Fact] void should_include_the_floor_itself() => ReleaseVersion.IsAtOrAfter("16.36.0", "16.36.0").ShouldBeTrue();
    [Fact] void should_include_a_later_patch_of_the_floor() => ReleaseVersion.IsAtOrAfter("16.36.1", "16.36.0").ShouldBeTrue();
    [Fact] void should_include_a_later_major() => ReleaseVersion.IsAtOrAfter("17.0.0", "16.36.0").ShouldBeTrue();
    [Fact] void should_treat_a_missing_component_as_zero() => ReleaseVersion.IsAtOrAfter("16.36", "16.36.0").ShouldBeTrue();
}
