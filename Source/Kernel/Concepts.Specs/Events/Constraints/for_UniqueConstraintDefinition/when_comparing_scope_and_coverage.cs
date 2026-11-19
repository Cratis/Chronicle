// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// A scope that scopes nothing means the same as no scope. A real scope, or other coverage, is a real change.
/// </summary>
public class when_comparing_scope_and_coverage : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _definition;

    void Establish() => _definition = new(_name, [new("the-event", ["A"])]);

    UniqueConstraintDefinition With(ConstraintScope? scope = null, bool ignoreCasing = false) => new(_name, [new("the-event", ["A"])], [], ignoreCasing, scope);

    [Fact] void should_treat_an_empty_scope_as_no_scope() => _definition.Equals(With(new ConstraintScope())).ShouldBeTrue();
    [Fact] void should_hash_an_empty_scope_as_no_scope() => _definition.GetHashCode().ShouldEqual(With(new ConstraintScope()).GetHashCode());
    [Fact] void should_report_no_change_for_an_empty_scope() => With(new ConstraintScope()).CompareWith(_definition).ShouldEqual(ConstraintChange.None);
    [Fact] void should_not_be_equal_to_a_real_scope() => _definition.Equals(With(new ConstraintScope(EventSourceType: "some-type"))).ShouldBeFalse();
    [Fact] void should_require_a_reindex_for_a_real_scope() => With(new ConstraintScope(EventSourceType: "some-type")).CompareWith(_definition).RequiresReindex.ShouldBeTrue();
    [Fact] void should_not_be_equal_when_ignoring_casing_differs() => _definition.Equals(With(ignoreCasing: true)).ShouldBeFalse();
    [Fact] void should_require_a_reindex_when_ignoring_casing_differs() => With(ignoreCasing: true).CompareWith(_definition).RequiresReindex.ShouldBeTrue();
    [Fact] void should_not_treat_every_event_sequence_as_the_event_log() => _definition.Equals(_definition with { EventSequences = [EventSequenceId.Log] }).ShouldBeFalse();
}
