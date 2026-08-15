// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScopes.when_checking_whether_all_should_be_validated;

/// <summary>
/// One skipped scope is enough to break the guarantee a caller believes the batch has, so a batch where any scope
/// went unchecked must not report the check as performed.
/// </summary>
public class and_one_scope_is_skipped : Specification
{
    ConcurrencyScopes _scopes;

    void Establish() => _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
    {
        { EventSourceId.New(), new ConcurrencyScope(42UL, true, null, null, null, null) },
        { EventSourceId.New(), new ConcurrencyScope(EventSequenceNumber.Unavailable, false, null, null, new EventSourceType("Thing"), null) }
    });

    [Fact] void should_not_report_that_all_are_validated() => _scopes.ShouldAllBeValidated.ShouldBeFalse();
}
