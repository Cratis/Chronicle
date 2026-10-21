// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_validate_only_and_a_protected_decision : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        Read();
        ValidateOnly();
        await Commit();
    });

    [Fact] void should_accept_the_owner_protected_decision_with_enforcement() => _error.ShouldBeNull();
    [Fact] void should_send_validation_without_events() => _request.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_requested_explicit_condition() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(100UL);
}
