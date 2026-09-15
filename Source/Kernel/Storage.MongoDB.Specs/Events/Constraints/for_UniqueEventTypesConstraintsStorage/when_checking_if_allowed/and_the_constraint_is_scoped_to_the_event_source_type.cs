// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A constraint scoped per event source type holds one cycle per event source type: the covered event already
/// appended blocks the next one carrying the same event source type, and says nothing about any other.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_constraint_is_scoped_to_the_event_source_type(MongoDBFixture fixture) : given.a_unique_event_types_constraints_storage(fixture)
{
    static readonly EventSourceType _loan = "loan";
    static readonly EventSourceType _reservation = "reservation";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(new ConstraintScope(EventSourceType: (EventSourceType)Marker));

    bool _isAllowedForTheSameEventSourceType;
    EventSequenceNumber _sequenceNumberForTheSameEventSourceType;
    bool _isAllowedForAnotherEventSourceType;

    async Task Establish() => await Append(_checkedOutEventType, _borrower, eventSourceType: _loan);

    async Task Because()
    {
        (_isAllowedForTheSameEventSourceType, _sequenceNumberForTheSameEventSourceType) = await _storage.IsAllowedWithinScope(_definition, _borrower, ScopeFor(_definition, eventSourceType: _loan));
        (_isAllowedForAnotherEventSourceType, _) = await _storage.IsAllowedWithinScope(_definition, _borrower, ScopeFor(_definition, eventSourceType: _reservation));
    }

    [Fact] void should_not_allow_a_covered_event_for_the_same_event_source_type() => _isAllowedForTheSameEventSourceType.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_holding_the_cycle() => _sequenceNumberForTheSameEventSourceType.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_allow_a_covered_event_for_another_event_source_type() => _isAllowedForAnotherEventSourceType.ShouldBeTrue();
}
