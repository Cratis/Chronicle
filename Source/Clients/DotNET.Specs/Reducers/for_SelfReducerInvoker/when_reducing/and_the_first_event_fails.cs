// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using static Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing.and_failure_preserves_progress_and_watcher_parity;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing;

public class and_the_first_event_fails : given.emitted_methods
{
    ReduceResult[] _results;
    IReducerObservers _observers;

    async Task Because()
    {
        var registry = Registry(typeof(Change));
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(Classic)).Returns(new ActivatedArtifact(new Classic(), typeof(Classic), NullLogger<ActivatedArtifact>.Instance));
        var invokers = new IReducerInvoker[]
        {
            new ReducerInvoker(registry, activator, typeof(Classic), typeof(ClassicState), "classic"),
            new SelfReducerInvoker(registry, typeof(SelfState), "self")
        };
        var store = Substitute.For<IEventStore>();
        _observers = Substitute.For<IReducerObservers>();
        _results = new ReduceResult[4];
        for (var index = 0; index < _results.Length; index++)
        {
            var invoker = invokers[index % 2];
            using var handler = new ReducerHandler(store, "reducer", invoker.ReadModelType, EventSequences.EventSequenceId.Log, invoker, true, _observers);
            var events = new[] { index < 2 ? -1 : -2, 100 }.Select((amount, position) => new AppendedEvent(EventContext.EmptyWithEventSourceId("one") with { SequenceNumber = (ulong)(position + 1) }, new Change(amount)));
            _results[index] = await handler.OnNext(events, null, Substitute.For<IServiceProvider>());
        }
    }

    [Fact] void should_fail_sync_and_async_folds_for_both_styles() => _results.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_leave_the_state_absent() => _results.All(result => result.ReadModelState is null).ShouldBeTrue();
    [Fact] void should_not_notify_classic_watchers_of_removal() => _observers.DidNotReceive().NotifyChange(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<ClassicState>(), Arg.Any<bool>());
    [Fact] void should_not_notify_self_watchers_of_removal() => _observers.DidNotReceive().NotifyChange(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<SelfState>(), Arg.Any<bool>());
}
