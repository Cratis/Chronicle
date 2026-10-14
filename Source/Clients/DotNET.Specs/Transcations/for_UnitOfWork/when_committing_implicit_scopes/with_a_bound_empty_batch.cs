// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_bound_empty_batch : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        _enrollment.AddEvents(EventSequenceId.Log, [], []);
    }
    Task Because() => Commit();
    [Fact] void should_keep_the_zero_event_append_path() => _request.ShouldNotBeNull();
    [Fact] void should_not_send_implicit_guards_without_events() => _request.ConcurrencyScopes.ShouldBeEmpty();
}
