// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Specs.Events.Constraints.for_IUniqueEventTypesConstraintsStorage.when_calling_the_scoped_member_on_a_provider_implementing_only_the_original_member;

/// <summary>
/// An unscoped constraint resolves to no scope at all, and the key an unscoped constraint used to arrive with was
/// the empty string. The provider must see exactly that, so its unscoped path is the one it always was - including
/// the case where it was called with the parameter omitted entirely.
/// </summary>
public class and_the_constraint_is_unscoped : Specification
{
    static readonly EventSourceId _borrower = "borrower";
    static readonly UniqueEventTypeConstraintDefinition _definition = new("loan-open", [new("LoanCheckedOut")], [new("LoanReturned")]);

    given.a_provider_implementing_only_the_original_member _provider;
    IUniqueEventTypesConstraintsStorage _storage;
    (bool IsAllowed, EventSequenceNumber SequenceNumber) _result;

    void Establish()
    {
        _provider = new() { Answer = (true, EventSequenceNumber.Unavailable) };
        _storage = _provider;
    }

    async Task Because() => _result = await _storage.IsAllowedWithinScope(_definition, _borrower);

    [Fact] void should_call_the_original_member_once() => _provider.TimesCalled.ShouldEqual(1);
    [Fact] void should_pass_the_empty_key_an_unscoped_constraint_always_carried() => _provider.ReceivedScopeKey.ShouldEqual(string.Empty);
    [Fact] void should_pass_the_same_key_as_an_unscoped_declaration_builds() => _provider.ReceivedScopeKey.ShouldEqual(ConstraintScope.None.BuildScopeKey(null, null, null));
    [Fact] void should_return_the_answer_the_provider_gave() => _result.ShouldEqual((true, EventSequenceNumber.Unavailable));
}
