// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion;

public class when_parsing_valid_version : Specification
{
    SemanticVersion _result;

    void Because() => _result = SemanticVersion.Parse("1.2.3");

    [Fact] void should_have_correct_major() => _result.Major.ShouldEqual(1);
    [Fact] void should_have_correct_minor() => _result.Minor.ShouldEqual(2);
    [Fact] void should_have_correct_patch() => _result.Patch.ShouldEqual(3);
    [Fact] void should_have_empty_prerelease() => _result.PreRelease.ShouldBeEmpty();
    [Fact] void should_have_empty_build_metadata() => _result.BuildMetadata.ShouldBeEmpty();
}
