// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScopes.when_checking_whether_all_should_be_validated;

/// <summary>
/// This is what the append result reports back as the concurrency check having been performed, so it has to mean
/// every event source in the batch had something compared against the event store - including the one whose scope
/// expects no matching event to exist yet.
/// </summary>
public class and_every_scope_asks_for_a_check : Specification
{
    ConcurrencyScopes _scopes;

    void Establish() => _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
    {
        { EventSourceId.New(), new ConcurrencyScope(42UL, true, null, null, null, null) },
        { EventSourceId.New(), new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, null, null) }
    });

    [Fact] void should_report_that_all_are_validated() => _scopes.ShouldAllBeValidated.ShouldBeTrue();
}
