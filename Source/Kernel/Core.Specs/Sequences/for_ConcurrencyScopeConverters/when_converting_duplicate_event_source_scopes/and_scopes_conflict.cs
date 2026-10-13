// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_duplicate_event_source_scopes;

public class and_scopes_conflict : Specification
{
    EventSourceConcurrencyScope[] _scopes;
    Result<ConcurrencyScopes, AppendError> _result;
    Exception? _error;

    void Establish() => _scopes =
    [
        new("source", new(0, true, ExpectsNoMatchingEvent: true)),
        new("source", new(5, true))
    ];

    void Because() => _error = Specifications.Catch.Exception(() => _result = _scopes.ToChronicle());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_refuse_conflicting_scopes() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_source_in_the_append_error() => _result.AsT1.Value.ShouldEqual("Conflicting concurrency scopes for event source 'source'.");
}
