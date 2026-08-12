// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_unspecified_scope_label : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes(
        [new(EventSourceId.New(), new object())],
        [new(EventSourceId.Unspecified, ConcurrencyScope.None)]));

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
}
