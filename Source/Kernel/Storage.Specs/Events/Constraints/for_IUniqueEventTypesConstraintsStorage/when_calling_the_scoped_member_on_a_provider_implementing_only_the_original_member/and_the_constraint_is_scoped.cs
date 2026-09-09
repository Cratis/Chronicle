// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Specs.Events.Constraints.for_IUniqueEventTypesConstraintsStorage.when_calling_the_scoped_member_on_a_provider_implementing_only_the_original_member;

/// <summary>
/// The kernel only ever calls the typed member now. A provider that never heard of it must still be called, and
/// must be handed the byte-exact key it would have been handed before - the same one
/// <see cref="ConstraintScopeExtensions.BuildScopeKey"/> builds for the same event - so its behavior is carried
/// over rather than changed. It is not thereby fixed: it still narrows by that key, with all the aliasing that
/// implies.
/// </summary>
public class and_the_constraint_is_scoped : Specification
{
    const string Marker = "_scoped_";
    static readonly EventSourceType _eventSourceType = "loan";
    static readonly EventStreamType _eventStreamType = "loans";
    static readonly EventStreamId _eventStreamId = "branch-1";
    static readonly EventSourceId _borrower = "borrower";
    static readonly ConstraintScope _declaration = new((EventSourceType)Marker, (EventStreamType)Marker, (EventStreamId)Marker);
    static readonly UniqueEventTypeConstraintDefinition _definition = new("loan-open", [new("LoanCheckedOut")], [new("LoanReturned")], _declaration);

    given.a_provider_implementing_only_the_original_member _provider;
    IUniqueEventTypesConstraintsStorage _storage;
    (bool IsAllowed, EventSequenceNumber SequenceNumber) _result;

    void Establish()
    {
        _provider = new() { Answer = (false, new EventSequenceNumber(7)) };
        _storage = _provider;
    }

    async Task Because() => _result = await _storage.IsAllowedWithinScope(
        _definition,
        _borrower,
        _declaration.ResolveFor(_eventSourceType, _eventStreamType, _eventStreamId));

    [Fact] void should_call_the_original_member_once() => _provider.TimesCalled.ShouldEqual(1);
    [Fact] void should_pass_the_definition_through() => _provider.ReceivedDefinition.ShouldEqual(_definition);
    [Fact] void should_pass_the_event_source_id_through() => _provider.ReceivedEventSourceId.ShouldEqual(_borrower);
    [Fact] void should_pass_the_exact_key_the_provider_used_to_receive() => _provider.ReceivedScopeKey.ShouldEqual(_declaration.BuildScopeKey(_eventSourceType, _eventStreamType, _eventStreamId));
    [Fact] void should_return_the_answer_the_provider_gave() => _result.ShouldEqual((false, new EventSequenceNumber(7)));
}
