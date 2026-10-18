// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_duplicate_event_source_scopes;

public class and_scopes_are_equal : Specification
{
    EventSourceConcurrencyScope[] _scopes;
    Result<ConcurrencyScopeConjunctions, AppendError> _result;
    Exception? _error;

    void Establish() => _scopes =
    [
        new("source", new(0, true, EventTypes: [new("event", 1, false), new("another-event", 1, false)], ExpectsNoMatchingEvent: true)),
        new("source", new(0, true, EventTypes: [new("another-event", 1, false), new("event", 1, false)], ExpectsNoMatchingEvent: true))
    ];

    void Because() => _error = Specifications.Catch.Exception(() => _result = _scopes.ToChronicle());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_merge_the_entries() => _result.AsT0.Entries.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_freshness_expectation() => _result.AsT0.Entries.Single().Scope.SequenceNumber.IsBeforeFirst.ShouldBeTrue();
}
