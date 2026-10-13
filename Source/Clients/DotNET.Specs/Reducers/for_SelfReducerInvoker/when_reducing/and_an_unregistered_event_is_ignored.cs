// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing;

public class and_an_unregistered_event_is_ignored : given.emitted_methods
{
    ReduceResult _result;
    readonly State _initial = new();

    async Task Because() => _result = await new SelfReducerInvoker(Registry(typeof(Joined)), typeof(State), "state").Invoke(Substitute.For<IServiceProvider>(), [new(new object(), EventContext.Empty with { SequenceNumber = 7 })], _initial);

    [Fact] void should_preserve_state() => ReferenceEquals(_result.ReadModelState, _initial).ShouldBeTrue();
    [Fact] void should_not_advance_progress() => _result.LastSuccessfullyObservedEvent.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    public record State
    {
        public static State Reduce(Joined @event, State? current) => new();
    }
}
