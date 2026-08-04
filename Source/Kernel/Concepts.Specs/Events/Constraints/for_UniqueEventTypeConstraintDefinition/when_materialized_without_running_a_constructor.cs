// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

/// <summary>
/// A document deserializer is free to materialize a record without running any constructor and without invoking any
/// initializer, assigning only the members the document actually carries - the MongoDB driver does exactly that. A
/// definition persisted before the constraint could cover several event types carries no value for them, so the
/// member is left as it was: null. Normalizing in the initializer therefore guards nothing on the one path that
/// needs guarding, which is how a shipped fix for exactly this came to have passing specs and no effect.
/// </summary>
public class when_materialized_without_running_a_constructor : Specification
{
    UniqueEventTypeConstraintDefinition _definition;
    EventTypeId[] _covered;
    Exception _equalityError;
    Exception _hashingError;
    Exception _enumerationError;

    void Establish() => _definition = (UniqueEventTypeConstraintDefinition)RuntimeHelpers.GetUninitializedObject(typeof(UniqueEventTypeConstraintDefinition));

    void Because()
    {
        _equalityError = Catch.Exception(() => _definition.Equals(new UniqueEventTypeConstraintDefinition("some-constraint", ["some-event-type"])));
        _hashingError = Catch.Exception(() => _definition.GetHashCode());
        _enumerationError = Catch.Exception(() => _covered = [.. _definition.EventTypeIds]);
    }

    [Fact] void should_cover_no_event_types() => _covered.ShouldBeEmpty();
    [Fact] void should_answer_equality_rather_than_throw() => _equalityError.ShouldBeNull();
    [Fact] void should_answer_hashing_rather_than_throw() => _hashingError.ShouldBeNull();
    [Fact] void should_be_enumerable_rather_than_throw() => _enumerationError.ShouldBeNull();
}
