// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class on_an_empty_log_ordered : given.a_unit_with_implicit_scopes
{
    protected override bool Ordered => true;
    void Establish()
    {
        Guard(EventSequenceNumber.BeforeFirst);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_check_that_no_matching_event_exists() => ScopeFor(_sourceId).ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_not_send_the_in_process_sentinel() => ScopeFor(_sourceId).SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
}
