// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion.when_comparing_versions;

public class and_left_is_greater_than_right : Specification
{
    SemanticVersion _smaller;
    SemanticVersion _larger;

    void Establish()
    {
        _smaller = new SemanticVersion(1, 0, 0);
        _larger = new SemanticVersion(2, 0, 0);
    }

    [Fact] void should_be_greater_than() => (_larger > _smaller).ShouldBeTrue();
    [Fact] void should_be_greater_than_or_equal() => (_larger >= _smaller).ShouldBeTrue();
    [Fact] void should_not_be_less_than() => (_larger < _smaller).ShouldBeFalse();
    [Fact] void should_not_be_less_than_or_equal() => (_larger <= _smaller).ShouldBeFalse();
    [Fact] void should_not_be_equal() => (_larger == _smaller).ShouldBeFalse();
}
