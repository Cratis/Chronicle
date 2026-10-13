// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing;

public class and_all_return_forms_share_null_semantics : given.emitted_methods
{
    object?[] _states;
    IServiceProvider _provider;
    EventSequenceNumber _position;

    async Task Because()
    {
        _provider = Substitute.For<IServiceProvider>();
        var invoker = new SelfReducerInvoker(Registry(typeof(Create), typeof(Update), typeof(Delete), typeof(AsyncDelete), typeof(Ignore)), typeof(State), "state");
        object[] events = [new Create(), new Update(), new Ignore(), new Delete(), new Create(), new AsyncDelete(), new Update()];
        _states = new object?[events.Length];
        object? current = null;
        for (var index = 0; index < events.Length; index++)
        {
            var result = await invoker.Invoke(_provider, [new(events[index], EventContext.Empty with { SequenceNumber = (ulong)index })], current);
            current = _states[index] = result.ReadModelState;
            _position = result.LastSuccessfullyObservedEvent;
        }
    }

    [Fact] void should_create_update_delete_and_recreate() => _states.Select(state => (state as State)?.Amount).ShouldEqual([1, 2, 2, null, 1, null, 1]);
    [Fact] void should_keep_the_current_reference_for_an_ignored_event() => ReferenceEquals(_states[1], _states[2]).ShouldBeTrue();
    [Fact] void should_never_resolve_a_model_or_service() => _provider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_report_recreation_progress() => _position.ShouldEqual((EventSequenceNumber)6UL);

    public record Create;
    public record Update;
    public record Delete;
    public record AsyncDelete;
    public record Ignore;
    public record State(int Amount)
    {
        public static State Reduce(Create @event, State? current) => new(1);
        public static Task<State> Reduce(Update @event, State? current) => Task.FromResult(new State((current?.Amount ?? 0) + 1));
        public static State? Reduce(Delete @event, State? current) => null;
        public static Task<State?> Reduce(AsyncDelete @event, State? current) => Task.FromResult<State?>(null);
        public static State? Reduce(Ignore @event, State? current) => current;
    }
}
