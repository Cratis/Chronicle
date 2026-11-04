// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReleaseVersion.when_validating;

public class and_the_version_is_well_formed : Specification
{
    [Fact] void should_accept_a_stable_release() => ReleaseVersion.IsValid("19.4.9").ShouldBeTrue();
    [Fact] void should_accept_a_pre_release() => ReleaseVersion.IsValid("19.4.9-hotfix.1").ShouldBeTrue();
    [Fact] void should_accept_build_metadata() => ReleaseVersion.IsValid("19.4.9+meta").ShouldBeTrue();
}
