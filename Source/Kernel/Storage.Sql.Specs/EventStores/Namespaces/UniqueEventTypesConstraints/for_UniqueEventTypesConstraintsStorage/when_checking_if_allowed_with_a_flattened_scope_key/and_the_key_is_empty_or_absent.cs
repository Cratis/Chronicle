// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueEventTypesConstraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed_with_a_flattened_scope_key;

/// <summary>
/// A caller compiled against the interface as it shipped keeps binding to the original member, and the overwhelming
/// majority of those calls carry no key at all - an unscoped constraint always arrived with the empty string, or
/// with the parameter omitted. Nothing is narrowed either way, which is precisely an unscoped typed lookup, so the
/// answer has to be identical to the one the typed member gives.
/// </summary>
public class and_the_key_is_empty_or_absent : given.a_unique_event_types_constraints_storage
{
    bool _isAllowedWithTheParameterOmitted;
    EventSequenceNumber _sequenceNumberWithTheParameterOmitted;
    bool _isAllowedWithAnEmptyKey;
    bool _isAllowedWithANullKey;
    bool _isAllowedThroughTheTypedMember;
    EventSequenceNumber _sequenceNumberThroughTheTypedMember;

    async Task Establish() => await Append(_checkedOutEventType, _borrower);

    async Task Because()
    {
        (_isAllowedWithTheParameterOmitted, _sequenceNumberWithTheParameterOmitted) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);
        (_isAllowedWithAnEmptyKey, _) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower, string.Empty);
        (_isAllowedWithANullKey, _) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower, null!);
        (_isAllowedThroughTheTypedMember, _sequenceNumberThroughTheTypedMember) = await _storage.IsAllowedWithinScope(DefinitionReleasedByReturn, _borrower);
    }

    [Fact] void should_answer_the_omitted_parameter_as_the_typed_member_does() => _isAllowedWithTheParameterOmitted.ShouldEqual(_isAllowedThroughTheTypedMember);
    [Fact] void should_report_the_same_sequence_number_as_the_typed_member() => _sequenceNumberWithTheParameterOmitted.ShouldEqual(_sequenceNumberThroughTheTypedMember);
    [Fact] void should_answer_an_empty_key_as_the_typed_member_does() => _isAllowedWithAnEmptyKey.ShouldEqual(_isAllowedThroughTheTypedMember);
    [Fact] void should_answer_a_null_key_as_the_typed_member_does() => _isAllowedWithANullKey.ShouldEqual(_isAllowedThroughTheTypedMember);
    [Fact] void should_still_refuse_the_covered_event_that_holds_the_cycle() => _isAllowedWithTheParameterOmitted.ShouldBeFalse();
}
