// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScopes.when_checking_whether_all_should_be_validated;

/// <summary>
/// Vacuous truth would be the wrong answer here. An append carrying no scopes had nothing compared against the event
/// store, and a caller reading this off the append result would take "all validated" as a guarantee it does not have.
/// </summary>
public class and_there_are_no_scopes_at_all : Specification
{
    ConcurrencyScopes _scopes;

    void Establish() => _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>());

    [Fact] void should_not_report_that_all_are_validated() => _scopes.ShouldAllBeValidated.ShouldBeFalse();
}
