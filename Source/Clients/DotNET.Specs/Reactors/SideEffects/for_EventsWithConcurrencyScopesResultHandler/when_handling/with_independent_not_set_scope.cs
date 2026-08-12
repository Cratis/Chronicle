// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class with_independent_not_set_scope : Specification
{
    Exception _error;
    IEventLog _eventLog;
    IEventStore _eventStore;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
    }

    async Task Because() => _error = await Catch.Exception(async () => await new EventsWithConcurrencyScopesResultHandler().Handle(
        new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        new EventsWithConcurrencyScopes(
            [new(EventSourceId.New(), new object())],
            [new(EventSourceId.New(), ConcurrencyScope.NotSet)])));

    [Fact] void should_reject_the_invalid_reactor_result() => _error.ShouldBeOfExactType<IndependentConcurrencyScopeMustBeExplicit>();
    [Fact] void should_not_append_any_reactor_side_effect() => _eventLog.DidNotReceiveWithAnyArgs().AppendMany(default!, default, default, default);
}
