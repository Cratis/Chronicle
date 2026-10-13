// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_receiving_replay_notifications;

public class and_self_hooks_keep_classic_order_and_scoped_lifetimes : an_explicit_store
{
    readonly Dictionary<string, Subject<ReduceOperationMessage>> _streams = [];
    Ledger _ledger;
    string[][] _blocked;
    string[][] _disposing;

    void Establish()
    {
        _provider.Dispose();
        _ledger = new();
        var services = new ServiceCollection().AddSingleton(_ledger);
        services.AddScoped<Work<SelfState>>().AddScoped<Work<Classic>>();
        services.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        services.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        services.AddKeyedSingleton<IActivitySource<Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers>>());
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _activator = new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterReducer<SelfState>().RegisterReducer<Classic>();
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            string id = null!;
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message =>
            {
                id = message.Content.Value0!.Reducer.ReducerId;
                _streams[id] = new();
            });
            return _streams[id];
        });
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.Reducers.Register();
        _blocked = new string[2][];
        _disposing = new string[2][];
        var index = 0;
        foreach (var type in new[] { typeof(SelfState), typeof(Classic) })
        {
            var trace = _ledger.For(type);
            var stream = _streams[_store.Reducers.GetHandlerFor(type).Id.Value];
            foreach (var state in new[] { ReplayState.BeginReplay, ReplayState.EndReplay, ReplayState.BeginReplayPartition, ReplayState.EndReplayPartition })
            {
                stream.OnNext(new() { ReplayState = state, Partition = "source" });
            }
            await trace.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _blocked[index] = trace.Actions.ToArray();
            trace.Release.SetResult();
            await trace.Disposing.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _disposing[index++] = trace.Actions.ToArray();
            trace.DisposeRelease.SetResult();
            await trace.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    void Destroy()
    {
        foreach (var handler in _store?.Reducers.GetAllHandlers() ?? [])
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
        foreach (var stream in _streams.Values) stream.Dispose();
    }

    [Fact] void should_await_each_begin_hook_before_later_notifications() => _blocked.All(actions => !actions.Contains("end")).ShouldBeTrue();
    [Fact] void should_await_async_service_disposal_before_later_notifications() => _disposing.All(actions => !actions.Contains("end")).ShouldBeTrue();
    [Fact] void should_preserve_classic_notification_order() => Notifications(typeof(Classic)).ShouldEqual(["begin", "end", "begin:source", "end:source"]);
    [Fact] void should_share_the_classic_notification_trace() => Notifications(typeof(SelfState)).ShouldEqual(Notifications(typeof(Classic)));
    [Fact] void should_never_activate_or_dispose_a_self_model() => _ledger.For(typeof(SelfState)).Actions.Where(action => action.StartsWith("model", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_preserve_classic_activation_per_notification() => _ledger.For(typeof(Classic)).Actions.Count(action => action == "model-created").ShouldEqual(4);
    [Fact] void should_preserve_classic_disposal_per_notification() => _ledger.For(typeof(Classic)).Actions.Count(action => action == "model-disposed").ShouldEqual(4);
    [Fact] void should_use_a_fresh_service_scope_for_every_self_hook() => _ledger.For(typeof(SelfState)).Actions.Where(action => action.StartsWith("service-created", StringComparison.Ordinal)).ShouldContainOnly("service-created:1", "service-created:2", "service-created:3", "service-created:4");
    [Fact] void should_dispose_every_self_service_after_its_hook() => _ledger.For(typeof(SelfState)).Actions.Count(action => action.StartsWith("service-disposed", StringComparison.Ordinal)).ShouldEqual(4);

    string[] Notifications(Type type) => _ledger.For(type).Actions.Where(action => new[] { "begin", "end", "begin:source", "end:source" }.Contains(action, StringComparer.Ordinal)).ToArray();

    public class Ledger
    {
        readonly ConcurrentDictionary<Type, Trace> _traces = new();
        public Trace For(Type type) => _traces.GetOrAdd(type, _ => new());
    }
    public class Trace
    {
        public ConcurrentQueue<string> Actions { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Disposing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DisposeRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Created;
        public int Disposed;
    }
    public class Work<T> : IAsyncDisposable
    {
        readonly int _id;
        public Trace Trace { get; }

        public Work(Ledger ledger)
        {
            Trace = ledger.For(typeof(T));
            _id = Interlocked.Increment(ref Trace.Created);
            Trace.Actions.Enqueue($"service-created:{_id}");
        }

        public async Task Begin()
        {
            Trace.Actions.Enqueue("begin");
            Trace.Entered.SetResult();
            await Trace.Release.Task;
            Trace.Actions.Enqueue("begin-completed");
        }

        public Task Record(string action)
        {
            Trace.Actions.Enqueue(action);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_id == 1)
            {
                Trace.Disposing.SetResult();
                await Trace.DisposeRelease.Task;
            }
            Trace.Actions.Enqueue($"service-disposed:{_id}");
            if (Interlocked.Increment(ref Trace.Disposed) == 4) Trace.Completed.SetResult();
        }
    }
    public record SelfState(string Name)
    {
        public static SelfState Reduce(PlainEvent @event, SelfState? current) => new(@event.Name);
        public static Task BeginReplay(Work<SelfState> service) => service.Begin();
        public static Task EndReplay(Work<SelfState> service) => service.Record("end");
        public static Task BeginReplayPartition(Partition partition, Work<SelfState> service) => service.Record($"begin:{partition.Value}");
        public static Task EndReplayPartition(Partition partition, Work<SelfState> service) => service.Record($"end:{partition.Value}");
    }
    public record ClassicState(string Name);
    public class Classic : IReducerFor<ClassicState>, ICanBeNotifiedWhenReplay, ICanBeNotifiedWhenPartitionReplayed, IAsyncDisposable
    {
        readonly Work<Classic> _service;
        public Classic(Work<Classic> service)
        {
            _service = service;
            service.Trace.Actions.Enqueue("model-created");
        }

        public ClassicState Apply(PlainEvent @event, ClassicState? current) => new(@event.Name);
        public Task BeginReplay() => _service.Begin();
        public Task EndReplay() => _service.Record("end");
        public Task BeginReplayPartition(Partition partition) => _service.Record($"begin:{partition.Value}");
        public Task EndReplayPartition(Partition partition) => _service.Record($"end:{partition.Value}");
        public ValueTask DisposeAsync()
        {
            _service.Trace.Actions.Enqueue("model-disposed");
            return ValueTask.CompletedTask;
        }
    }
}
