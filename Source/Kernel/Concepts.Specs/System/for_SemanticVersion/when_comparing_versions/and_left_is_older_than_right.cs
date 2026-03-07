// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion.when_comparing_versions;

public class and_left_is_older_than_right : Specification
{
    SemanticVersion _older;
    SemanticVersion _newer;

    void Establish()
    {
        _older = new SemanticVersion(1, 0, 0);
        _newer = new SemanticVersion(2, 0, 0);
    }

    [Fact] void should_be_less_than() => (_older < _newer).ShouldBeTrue();
    [Fact] void should_be_less_than_or_equal() => (_older <= _newer).ShouldBeTrue();
    [Fact] void should_not_be_greater_than() => (_older > _newer).ShouldBeFalse();
    [Fact] void should_not_be_greater_than_or_equal() => (_older >= _newer).ShouldBeFalse();
    [Fact] void should_not_be_equal() => (_older == _newer).ShouldBeFalse();
}
