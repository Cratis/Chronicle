// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion;

public class when_converting_to_string : Specification
{
    SemanticVersion _version;
    string _result;

    void Establish() => _version = new SemanticVersion(1, 2, 3, "alpha.1", "build.123");

    void Because() => _result = _version.ToString();

    [Fact] void should_have_correct_format() => _result.ShouldEqual("1.2.3-alpha.1+build.123");
}
