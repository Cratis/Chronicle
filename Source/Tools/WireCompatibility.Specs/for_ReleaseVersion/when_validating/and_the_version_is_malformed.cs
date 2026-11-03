// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_validating;

public class and_the_version_is_malformed : Specification
{
    [Fact] void should_refuse_a_tag_prefix() => ReleaseVersion.IsValid("v19.4.9").ShouldBeFalse();
    [Fact] void should_refuse_a_missing_patch() => ReleaseVersion.IsValid("19.4").ShouldBeFalse();
    [Fact] void should_refuse_a_major_alone() => ReleaseVersion.IsValid("19").ShouldBeFalse();
    [Fact] void should_refuse_a_non_numeric_component() => ReleaseVersion.IsValid("19.x.0").ShouldBeFalse();
    [Fact] void should_refuse_a_version_alias() => ReleaseVersion.IsValid("latest").ShouldBeFalse();
    [Fact] void should_refuse_an_empty_version() => ReleaseVersion.IsValid(string.Empty).ShouldBeFalse();
    [Fact] void should_refuse_an_empty_suffix() => ReleaseVersion.IsValid("19.4.9-").ShouldBeFalse();
}
