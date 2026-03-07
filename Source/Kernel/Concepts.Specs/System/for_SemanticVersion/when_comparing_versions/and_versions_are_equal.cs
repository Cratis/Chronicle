// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion.when_comparing_versions;

public class and_versions_are_equal : Specification
{
    SemanticVersion _first;
    SemanticVersion _second;

    void Establish()
    {
        _first = new SemanticVersion(1, 5, 3);
        _second = new SemanticVersion(1, 5, 3);
    }

    [Fact] void should_be_equal() => (_first == _second).ShouldBeTrue();
    [Fact] void should_be_less_than_or_equal() => (_first <= _second).ShouldBeTrue();
    [Fact] void should_be_greater_than_or_equal() => (_first >= _second).ShouldBeTrue();
    [Fact] void should_not_be_less_than() => (_first < _second).ShouldBeFalse();
    [Fact] void should_not_be_greater_than() => (_first > _second).ShouldBeFalse();
}
