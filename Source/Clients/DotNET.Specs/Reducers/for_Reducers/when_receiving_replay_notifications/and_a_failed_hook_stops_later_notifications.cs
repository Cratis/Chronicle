// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_receiving_replay_notifications;

public class and_a_failed_hook_stops_later_notifications : an_explicit_store
{
    readonly Dictionary<string, Subject<ReduceOperationMessage>> _streams = [];
    readonly ConcurrentQueue<string> _logs = new();
    Ledger _ledger;

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
        foreach (var type in new[] { typeof(SelfState), typeof(Classic) })
        {
            var handler = _store.Reducers.GetHandlerFor(type);
            _ledger.For(type).Id = handler.Id.Value;
        }

        // Install diagnostics before registration and the first hook-entry wait.
        typeof(Reducers).GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_store.Reducers, new FailureLogger(_logs, (id, error) =>
            {
                var trace = _ledger.ById(id);

                // The error has already terminated Concat. Cancel only its scheduled reconnect.
                _store.Reducers.GetHandlerById(id).Disconnect();
                trace.Failed.TrySetResult(error);
            }));
        await _store.Reducers.Register();
        foreach (var type in new[] { typeof(SelfState), typeof(Classic) })
        {
            var trace = _ledger.For(type);
            var stream = _streams[trace.Id];
            stream.OnNext(new() { ReplayState = ReplayState.BeginReplay, Partition = "source" });
            stream.OnNext(new() { ReplayState = ReplayState.EndReplay, Partition = "source" });
            try
            {
                await trace.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                trace.Release.SetException(new HookFailed());
                trace.Error = await trace.Failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                throw new HookTraceFailed(type, trace.Actions, _logs);
            }
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

    [Fact] void should_route_the_classic_hook_fault_to_the_native_stream_failure_path() => _ledger.For(typeof(Classic)).Error.ShouldBeOfExactType<HookFailed>();
    [Fact] void should_route_the_self_hook_fault_to_the_same_failure_path() => _ledger.For(typeof(SelfState)).Error.ShouldBeOfExactType<HookFailed>();
    [Fact] void should_preserve_the_classic_termination_of_queued_notifications() => _ledger.For(typeof(Classic)).Actions.ToArray().ShouldEqual(["begin", "disposed"]);
    [Fact] void should_share_the_classic_failure_trace_without_promising_an_end_hook() => _ledger.For(typeof(SelfState)).Actions.ToArray().ShouldEqual(_ledger.For(typeof(Classic)).Actions.ToArray());

    public class Ledger
    {
        readonly ConcurrentDictionary<Type, Trace> _traces = new();
        public Trace For(Type type) => _traces.GetOrAdd(type, _ => new());
        public Trace ById(string id) => _traces.Values.Single(trace => trace.Id == id);
    }
    public class Trace
    {
        public string Id { get; set; } = string.Empty;
        public ConcurrentQueue<string> Actions { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Exception> Failed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception Error { get; set; } = null!;
    }
    public class Work<T>(Ledger ledger) : IAsyncDisposable
    {
        readonly Trace _trace = ledger.For(typeof(T));
        public async Task Begin()
        {
            _trace.Actions.Enqueue("begin");
            _trace.Entered.TrySetResult();
            await _trace.Release.Task;
        }
        public Task End()
        {
            _trace.Actions.Enqueue("end");
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            _trace.Actions.Enqueue("disposed");
            return ValueTask.CompletedTask;
        }
    }
    public record ClassicState(string Name);
    public class Classic(Work<Classic> work) : IReducerFor<ClassicState>, ICanBeNotifiedWhenReplay
    {
        public ClassicState Apply(PlainEvent @event, ClassicState? current) => new(@event.Name);
        public Task BeginReplay() => work.Begin();
        public Task EndReplay() => work.End();
    }
    public record SelfState(string Name)
    {
        public static SelfState Reduce(PlainEvent @event, SelfState? current) => new(@event.Name);
        public static Task BeginReplay(Work<SelfState> work) => work.Begin();
        public static Task EndReplay(Work<SelfState> work) => work.End();
    }
    public class HookFailed : Exception;
    public class HookTraceFailed(Type type, IEnumerable<string> actions, IEnumerable<string> logs)
        : Exception($"Hook trace {type.FullName}: {string.Join(';', actions)}; logs: {string.Join(';', logs)}");
    public class FailureLogger(ConcurrentQueue<string> logs, Action<string, Exception> failed) : ILogger<Reducers>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            logs.Enqueue($"{logLevel}:{formatter(state, exception)}:{exception?.GetType().Name}");
            if (exception is HookFailed && state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                failed(values.Single(value => value.Key == "ReducerId").Value!.ToString()!, exception);
            }
        }
    }
}
