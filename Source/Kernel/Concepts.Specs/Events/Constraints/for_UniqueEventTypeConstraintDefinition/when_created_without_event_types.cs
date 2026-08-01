// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

/// <summary>
/// A definition persisted before the constraint could cover several event types has no value for them at all.
/// Storage upgrades such a definition on read; this is the backstop for anything that reaches the domain
/// without going through that path, so that equality and hashing answer rather than throw.
/// </summary>
public class when_created_without_event_types : Specification
{
    readonly ConstraintName _name = "some-constraint";

    UniqueEventTypeConstraintDefinition _definition;

    void Because() => _definition = new(_name, null!);

    [Fact] void should_cover_no_event_types() => _definition.EventTypeIds.ShouldBeEmpty();
    [Fact] void should_equal_a_definition_covering_no_event_types() => _definition.Equals(new UniqueEventTypeConstraintDefinition(_name, [])).ShouldBeTrue();
    [Fact] void should_hash_as_a_definition_covering_no_event_types() => _definition.GetHashCode().ShouldEqual(new UniqueEventTypeConstraintDefinition(_name, []).GetHashCode());
}
