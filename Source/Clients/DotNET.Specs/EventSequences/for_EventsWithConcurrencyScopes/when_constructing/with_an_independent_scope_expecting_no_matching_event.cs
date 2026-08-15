// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

/// <summary>
/// An independent label - one no event in the batch targets - must carry a scope the kernel can actually check,
/// because nothing will resolve it later. A scope expecting no matching event to exist is such a scope, which is
/// what makes "hold this partition empty while I write elsewhere" expressible at all. It used to be rejected here,
/// along with the scopes that carry nothing to compare against.
/// </summary>
public class with_an_independent_scope_expecting_no_matching_event : Specification
{
    static readonly EventSourceId _independentLabel = EventSourceId.New();

    EventsWithConcurrencyScopes _result;
    Exception _error;

    void Because() => _error = Catch.Exception(() => _result = new EventsWithConcurrencyScopes(
        [new(EventSourceId.New(), new object())],
        [new(_independentLabel, new ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventTypes: [new EventType("event", 1)]))]));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_keep_the_independent_scope() => _result.ConcurrencyScopes.ContainsKey(_independentLabel).ShouldBeTrue();
    [Fact] void should_keep_it_expecting_no_matching_event() => _result.ConcurrencyScopes[_independentLabel].ExpectsNoMatchingEvent.ShouldBeTrue();
}
