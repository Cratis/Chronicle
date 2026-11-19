// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// The client rebuilds the definition from reflected attributes on every connect, so the order its collections come in
/// is not part of what it means. A definition listing the same things in another order is the same definition.
/// </summary>
public class when_comparing_with_a_reordered_definition : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _definition;
    UniqueConstraintDefinition _reordered;

    void Establish()
    {
        _definition = new(
            _name,
            [new("first-event", ["A", "B"]), new("second-event", ["C"])],
            ["removed-one", "removed-two"])
        {
            RemovalEventDefinitions = [new("removed-one", ["A"]), new("removed-two", ["C"])],
            EventSequences = ["first-sequence", "second-sequence"]
        };
        _reordered = new(
            _name,
            [new("second-event", ["C"]), new("first-event", ["A", "B"])],
            ["removed-two", "removed-one", "removed-two"])
        {
            RemovalEventDefinitions = [new("removed-two", ["C"]), new("removed-one", ["A"])],
            EventSequences = ["second-sequence", "first-sequence", "first-sequence"]
        };
    }

    [Fact] void should_be_equal() => _definition.Equals(_reordered).ShouldBeTrue();
    [Fact] void should_be_equal_the_other_way_around() => _reordered.Equals(_definition).ShouldBeTrue();
    [Fact] void should_hash_alike() => _definition.GetHashCode().ShouldEqual(_reordered.GetHashCode());
    [Fact] void should_report_no_change() => _reordered.CompareWith(_definition).ShouldEqual(ConstraintChange.None);
    [Fact] void should_normalize_to_the_same_definition() => _reordered.Normalized().EventDefinitions.SequenceEqual(_definition.Normalized().EventDefinitions).ShouldBeTrue();
}
