// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueEventTypesConstraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// Two genuinely different scopes must stay different. The appended event and the one being validated carry
/// different event source types and different event stream types, yet joining those dimensions into one delimited
/// string yields the exact same text for both, so a provider narrowing by such a key would treat the appended event
/// as belonging to this cycle and refuse a legitimate append. Comparing each dimension on its own keeps them apart.
/// </summary>
public class and_the_scope_dimensions_would_collide_under_a_flat_key : given.a_unique_event_types_constraints_storage
{
    static readonly EventSourceType _appendedSourceType = "loan|estt:branch";
    static readonly EventStreamType _appendedStreamType = "region";
    static readonly EventSourceType _validatedSourceType = "loan";
    static readonly EventStreamType _validatedStreamType = "branch|estt:region";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(new ConstraintScope((EventSourceType)Marker, (EventStreamType)Marker));

    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish() => await Append(_checkedOutEventType, _borrower, _appendedSourceType, _appendedStreamType);

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, _validatedSourceType, _validatedStreamType));

    [Fact] void should_allow_the_covered_event_in_the_genuinely_different_scope() => _isAllowed.ShouldBeTrue();
    [Fact] void should_have_no_sequence_number_to_report() => _sequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);

    [Fact]
    void should_have_scopes_that_are_indistinguishable_once_flattened() =>
        _definition.Scope.BuildScopeKey(_appendedSourceType, _appendedStreamType, null)
            .ShouldEqual(_definition.Scope.BuildScopeKey(_validatedSourceType, _validatedStreamType, null));
}
