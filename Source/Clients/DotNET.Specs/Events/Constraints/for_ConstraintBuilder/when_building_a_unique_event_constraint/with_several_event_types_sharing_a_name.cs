// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_a_unique_event_constraint;

/// <summary>
/// Declaring several event types under one constraint name is how mutual exclusion is expressed — an event
/// source terminal through either outcome may have one of them, never both and never twice. The definitions
/// merge into a single constraint so that names stay unique across the built set.
/// </summary>
public class with_several_event_types_sharing_a_name : given.a_constraint_builder_with_owner
{
    const string ConstraintName = "PersonTerminal";

    IImmutableList<IConstraintDefinition> _result;
    EventType _aliasedEventType;
    EventType _erasedEventType;

    void Establish()
    {
        _aliasedEventType = new EventType(nameof(PersonAliasedTo), EventTypeGeneration.First);
        _erasedEventType = new EventType(nameof(PersonErased), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(PersonAliasedTo)).Returns(_aliasedEventType);
        _eventTypes.GetEventTypeFor(typeof(PersonErased)).Returns(_erasedEventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<PersonAliasedTo>(name: ConstraintName);
        _constraintBuilder.Unique<PersonErased>(name: ConstraintName);
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_merge_into_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_keep_the_shared_name() => _result[0].Name.Value.ShouldEqual(ConstraintName);
    [Fact] void should_cover_both_event_types() =>
        ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_aliasedEventType.Id, _erasedEventType.Id]);

    record PersonAliasedTo();
    record PersonErased();
}
