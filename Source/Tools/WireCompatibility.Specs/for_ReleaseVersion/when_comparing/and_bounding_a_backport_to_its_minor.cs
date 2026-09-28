// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_comparing;

public class and_bounding_a_backport_to_its_minor : Specification
{
    [Fact] void should_include_earlier_minors() => ReleaseVersion.IsAtOrBeforeMinor("19.3.8", 4).ShouldBeTrue();
    [Fact] void should_include_the_current_minor() => ReleaseVersion.IsAtOrBeforeMinor("19.4.8", 4).ShouldBeTrue();
    [Fact] void should_exclude_later_minors() => ReleaseVersion.IsAtOrBeforeMinor("19.14.1", 4).ShouldBeFalse();
}
