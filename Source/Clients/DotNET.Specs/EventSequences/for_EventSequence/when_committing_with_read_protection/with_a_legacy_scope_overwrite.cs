// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_legacy_scope_overwrite : given.a_trusted_commit
{
    Task Because() => Execute(() =>
    {
        _unit.AddEvent(EventSequenceId.Log, "source", "first", Causation.Unknown(), concurrencyScope: new(100, "source"));
        _unit.AddEvent(EventSequenceId.Log, "source", "second", Causation.Unknown(), concurrencyScope: new(101, "source"));
        return Task.CompletedTask;
    });

    [Fact] void should_refuse_instead_of_overwriting_the_explicit_scope() => _error.ShouldBeOfExactType<ConflictingContributionsOnLabel>();
    [Fact] void should_keep_only_the_first_staged_event() => _unit.GetEvents().Single().ShouldEqual("first");
    [Fact] void should_not_append() => _request.ShouldBeNull();
}
