// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeBuilder;

public class when_scoping_to_an_unknown_stream : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new ConcurrencyScopeBuilder().ForEventSource<EventSources.for_EventSources.ShoppingCartEventSource>("Nope"));

    [Fact] void should_reject_it() => _exception.ShouldBeOfExactType<EventSources.EventStreamDoesNotBelongToEventSource>();
}
