// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

public class when_comparing_with_a_different_mode : Specification
{
    UniqueConstraintDefinition _original;
    UniqueConstraintDefinition _changed;
    ConstraintChange _result;

    void Establish()
    {
        _original = new("version", [new("added", ["id"])]);
        _changed = _original with { Mode = UniqueConstraintMode.PerValue };
    }

    void Because() => _result = _changed.CompareWith(_original);

    [Fact] void should_require_reindexing() => _result.ChangeTypes.ShouldContain(ConstraintChangeType.IndexedPropertiesChanged);
    [Fact] void should_not_equal_the_previous_definition() => _changed.Equals(_original).ShouldBeFalse();
}
