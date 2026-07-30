// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_a_unique_event_constraint;

/// <summary>
/// Merging must not let the same event type accumulate — a duplicate declaration is redundant, not a second
/// participant, and a repeated id would change the constraint's content-derived version on every rebuild.
/// </summary>
public class with_the_same_event_type_declared_twice : given.a_constraint_builder_with_owner
{
    const string ConstraintName = "PersonTerminal";

    IImmutableList<IConstraintDefinition> _result;
    EventType _eventType;

    void Establish()
    {
        _eventType = new EventType(nameof(PersonErased), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(PersonErased)).Returns(_eventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<PersonErased>(name: ConstraintName);
        _constraintBuilder.Unique<PersonErased>(name: ConstraintName);
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_merge_into_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_cover_the_event_type_once() =>
        ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_eventType.Id]);

    record PersonErased();
}
