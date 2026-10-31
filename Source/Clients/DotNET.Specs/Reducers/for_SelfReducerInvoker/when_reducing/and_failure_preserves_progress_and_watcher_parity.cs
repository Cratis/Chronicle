// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing;

public class and_failure_preserves_progress_and_watcher_parity : given.emitted_methods
{
    ReduceResult[] _results;
    IReducerObservers _observers;

    async Task Because()
    {
        var registry = Registry(typeof(Change));
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Classic)).Returns(new ActivatedArtifact(new Classic(), typeof(Classic), NullLogger<ActivatedArtifact>.Instance));
        var classic = new ReducerInvoker(registry, activator, typeof(Classic), typeof(ClassicState), "classic");
        var self = new SelfReducerInvoker(registry, typeof(SelfState), "self");
        var store = Substitute.For<IEventStore>();
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _observers = Substitute.For<IReducerObservers>();
        var invokers = new IReducerInvoker[] { classic, self };
        _results = new ReduceResult[4];
        for (var index = 0; index < _results.Length; index++)
        {
            var invoker = invokers[index % 2];
            using var handler = new ReducerHandler(store, "reducer", invoker.ReadModelType, EventSequences.EventSequenceId.Log, invoker, true, _observers);
            var events = new[] { 1, index < 2 ? -1 : -2, 100 }.Select((amount, position) => new AppendedEvent(EventContext.EmptyWithEventSourceId("one") with { SequenceNumber = (ulong)(position + 1) }, new Change(amount)));
            _results[index] = await handler.OnNext(events, null, Substitute.For<IServiceProvider>());
        }
    }

    [Fact] void should_fail_sync_and_async_folds_for_both_styles() => _results.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_keep_only_pre_failure_state() => _results.Select(result => ((IState)result.ReadModelState!).Amount).ShouldEqual([1, 1, 1, 1]);
    [Fact] void should_stop_progress_before_the_failure() => _results.All(result => result.LastSuccessfullyObservedEvent == (EventSequenceNumber)1UL).ShouldBeTrue();
    [Fact] void should_not_notify_classic_watchers_on_failure() => _observers.DidNotReceive().NotifyChange(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<ClassicState>(), Arg.Any<bool>());
    [Fact] void should_not_notify_self_watchers_on_failure() => _observers.DidNotReceive().NotifyChange(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<SelfState>(), Arg.Any<bool>());

    public interface IState
    {
        int Amount { get; }
    }
    public record Change(int Amount);
    public record ClassicState(int Amount) : IState;
    public class Classic : IReducerFor<ClassicState>
    {
        public Task<ClassicState> Apply(Change @event, ClassicState? current) => @event.Amount switch
        {
            -1 => throw new FoldFailed(),
            -2 => Task.FromException<ClassicState>(new FoldFailed()),
            _ => Task.FromResult(new ClassicState((current?.Amount ?? 0) + @event.Amount))
        };
    }
    public record SelfState(int Amount) : IState
    {
        public static Task<SelfState> Reduce(Change @event, SelfState? current) => @event.Amount switch
        {
            -1 => throw new FoldFailed(),
            -2 => Task.FromException<SelfState>(new FoldFailed()),
            _ => Task.FromResult(new SelfState((current?.Amount ?? 0) + @event.Amount))
        };
    }
    public class FoldFailed : Exception;
}
