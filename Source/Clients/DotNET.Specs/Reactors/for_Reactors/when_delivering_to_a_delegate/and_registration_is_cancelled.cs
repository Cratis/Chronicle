// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

using ContractAppendedEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_to_a_delegate;

public class and_registration_is_cancelled : given.all_dependencies
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CompletionLogger _completionLogger = new();
    Subject<EventsToObserve> _observed;
    int _publishedResults;

    protected override ILogger<Reactors> CreateLogger() => _completionLogger;

    void Establish()
    {
        _observed = new Subject<EventsToObserve>();
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(call =>
            {
                call.Arg<IObservable<ReactorMessage>>().Subscribe(message =>
                {
                    if (message.Content.Value is ReactorResult)
                    {
                        Interlocked.Increment(ref _publishedResults);
                    }
                });
                return _observed;
            });
    }

    async Task Because()
    {
        await _reactors.Register("cancelled-bridge", builder => builder.WithEventType(new EventType("orders", 1)), async (_, token) =>
        {
            _started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            finally
            {
                _cancelled.TrySetResult();
            }
        });
        _observed.OnNext(new EventsToObserve
        {
            Partition = "order-42",
            Events = [new ContractAppendedEvent
            {
                Context = (EventContext.Empty with { EventType = new EventType("orders", 1), SequenceNumber = 12 }).ToContract(),
                Content = "{\"order\":42}"
            }]
        });
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _reactors.Unregister("cancelled-bridge");
        await _cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _completionLogger.Completed.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_publish_a_failed_result() => _publishedResults.ShouldEqual(0);
    [Fact] void should_not_log_a_handling_error() => _completionLogger.HandlingErrors.ShouldEqual(0);

    sealed class CompletionLogger : ILogger<Reactors>
    {
        readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _handlingErrors;

        public Task Completed => _completed.Task;
        public int HandlingErrors => _handlingErrors;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => EmptyScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (logLevel == LogLevel.Trace && message.StartsWith("Handling of events received for Reactor", StringComparison.Ordinal))
            {
                _completed.TrySetResult();
            }
            if (logLevel == LogLevel.Warning && message.StartsWith("An error occurred while handling event", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _handlingErrors);
            }
        }

        sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
