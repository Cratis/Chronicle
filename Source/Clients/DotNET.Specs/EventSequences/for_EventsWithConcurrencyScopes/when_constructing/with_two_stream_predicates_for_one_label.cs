// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_two_stream_predicates_for_one_label : Specification
{
    EventsWithConcurrencyScopes _batch;
    Exception _dictionaryError;

    void Establish() => _batch = new(
        [new("report", new object())],
        [new("report", new ConcurrencyScope(7, "report", "stream", "first")), new("report", new ConcurrencyScope(9, "report", "stream", "second"))]);

    void Because() => _dictionaryError = Catch.Exception(() => _ = _batch.ConcurrencyScopes);

    [Fact] void should_retain_both_ordered_entries() => _batch.Entries.Count.ShouldEqual(2);
    [Fact] void should_never_drop_a_guard_through_the_legacy_dictionary() => _dictionaryError.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
}
