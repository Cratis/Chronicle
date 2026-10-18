// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_not_set_and_an_explicit_stream_guard : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_mixed_strategy_and_explicit_guards(bool notSetFirst)
    {
        KeyValuePair<Events.EventSourceId, ConcurrencyScope>[] scopes =
            [new("source", ConcurrencyScope.NotSet), new("source", new ConcurrencyScope(42, "source", "stream", "first"))];
        var error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes([new("source", new object())], notSetFirst ? scopes : scopes.Reverse()));

        error.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
    }
}
