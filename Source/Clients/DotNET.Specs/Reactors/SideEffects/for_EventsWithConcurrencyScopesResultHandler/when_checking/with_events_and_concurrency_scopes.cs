// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_checking;

public class with_events_and_concurrency_scopes : Specification
{
    bool _result;

    void Because() => _result = new EventsWithConcurrencyScopesResultHandler().CanHandle(
        new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        new EventsWithConcurrencyScopes([], []));

    [Fact] void should_handle_the_value() => _result.ShouldBeTrue();
}
