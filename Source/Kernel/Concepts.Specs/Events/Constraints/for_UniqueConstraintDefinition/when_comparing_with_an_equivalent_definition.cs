// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// Registration decides whether a constraint changed by comparing the incoming definition with the stored one,
/// and the incoming one is rebuilt from the client's attributes on every connect. So the comparison has to be by
/// content: comparing the covered events by reference makes every re-registration look like a change.
/// </summary>
/// <remarks>
/// The sibling unique-event-type definition already says exactly this in its own remarks and compares its
/// covered event types by content. This one did not, so the same startup that left that constraint alone
/// reported this one as changed - persisting another version of an identical definition and asking for a
/// reindex - every time the process started.
/// </remarks>
public class when_comparing_with_an_equivalent_definition : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _definition;
    UniqueConstraintDefinition _equivalent;

    void Establish()
    {
        _definition = new(_name, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])]);
        _equivalent = new(_name, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])]);
    }

    [Fact] void should_be_equal() => _definition.Equals(_equivalent).ShouldBeTrue();
    [Fact] void should_be_equal_through_the_interface() => _definition.Equals((IConstraintDefinition)_equivalent).ShouldBeTrue();
    [Fact] void should_hash_alike() => _definition.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_report_no_change() => _definition.CompareWith(_equivalent).ShouldEqual(ConstraintChange.None);

    [Fact]
    void should_not_be_equal_to_one_covering_another_property() =>
        _definition.Equals(new UniqueConstraintDefinition(_name, [new UniqueConstraintEventDefinition("the-event-type", ["another-property"])])).ShouldBeFalse();

    [Fact]
    void should_not_be_equal_to_one_covering_another_event_type() =>
        _definition.Equals(new UniqueConstraintDefinition(_name, [new UniqueConstraintEventDefinition("another-event-type", ["the-property"])])).ShouldBeFalse();
}
