// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_read_then_validate_only_intent : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        EnrollReadScope();
        ValidateOnly();
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_append_validation_only() => _request.Events.ShouldBeEmpty();
    [Fact] void should_send_the_explicit_boundary() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(100UL);
}
