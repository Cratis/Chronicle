// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.for_Reactors.when_reading_observer_information.and_reads_overlap_an_observe_stream_reconnection.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_reading_observer_information;

/// <summary>
/// Reads the state of a reactor while the connection drops and reconnects underneath it. Every round holds the
/// previous generation's disconnect handlers open, so the new generation can only register and connect once they
/// have been released - the spec proves the order rather than hoping the timing produces it.
/// </summary>
/// <param name="context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reads_overlap_an_observe_stream_reconnection(context context) : Given<context>(context)
{
    const string SkipReason = "Needs the out-of-process kernel: the transport only exists there.";

    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public const int Rounds = 4;
        public const int Readers = 16;
        public bool RanOutOfProcess;
        public ReactorWithoutDelay Reactor;
        public List<ReactorState> States = [];
        public List<Exception> Errors = [];
        public List<bool> ReadersPending = [];
        public List<bool> ProbeSawDisconnectCompleted = [];
        public List<bool> ProbeSawNewGeneration = [];
        public List<bool> RegistrationSucceeded = [];
        public List<bool> FenceLoggedBeforeNewGeneration = [];
        public ReactorState FinalState;
        public EventSequenceNumber LastSequenceNumber;

        volatile Round _round = new();

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithoutDelay)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            if (ChronicleFixture is not ChronicleFixture configurable || configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess)
            {
                return;
            }

            RanOutOfProcess = true;
            using var loggerFactory = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddProvider(new SignallingLoggerProvider(() => _round.FenceLogged.TrySetResult())));
            using var client = CreateClient(configurable, loggerFactory);
            var eventStore = await client.GetEventStore(EventStore.Name, EventStore.Namespace);
            (await eventStore.WaitForRegistration(TimeSpanFactory.FromSeconds(30))).IsSuccess.ShouldBeTrue();
            eventStore.Connection.Lifecycle.OnDisconnected += OnDisconnected;
            eventStore.Connection.Lifecycle.OnConnected += OnConnected;

            var handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await handler.WaitTillActive();
            var firstAppend = await eventStore.EventLog.Append("reactor", new SomeEvent(1));
            await handler.WaitTillReachesEventSequenceNumber(firstAppend.SequenceNumber);
            for (var round = 0; round < Rounds; round++)
            {
                await ReadAcrossReconnection(eventStore);
            }

            handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            var lastAppend = await eventStore.EventLog.Append("reactor", new SomeEvent(2));
            LastSequenceNumber = lastAppend.SequenceNumber;
            await handler.WaitTillReachesEventSequenceNumber(LastSequenceNumber);
            FinalState = await handler.WaitTillActiveAndGetState();
        }

        async Task ReadAcrossReconnection(IEventStore eventStore)
        {
            var connection = eventStore.Connection;
            var handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            var round = new Round { Connection = connection, Before = connection.Lifecycle.ConnectionId };
            _round = round;

            // The production path: the watchdog drops the session, disconnects, then reconnects. Awaiting the
            // handler having started proves the disconnect happened rather than merely having been scheduled.
            connection.RequestReconnect();
            await round.DisconnectObserved.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));

            var reads = Enumerable.Range(0, Readers).Select(_ => ReadState(handler, round)).ToArray();
            await round.ReadersEntered.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));

            // Whichever comes first releases the latch, so the spec finishes on broken code too and the facts report it.
            await Task.WhenAny(round.FenceLogged.Task, round.NewGeneration.Task).WaitAsync(TimeSpanFactory.FromSeconds(30));
            FenceLoggedBeforeNewGeneration.Add(round.FenceLogged.Task.IsCompleted);
            round.ReleaseDisconnect.TrySetResult();

            var results = await Task.WhenAll(reads).WaitAsync(TimeSpanFactory.FromSeconds(60));
            await round.NewGeneration.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
            States.AddRange(results.Select(_ => _.State).OfType<ReactorState>());
            Errors.AddRange(results.Select(_ => _.Error).OfType<Exception>());
            ReadersPending.AddRange(round.PendingWhenIssued);
            ProbeSawDisconnectCompleted.Add(round.ProbeSawDisconnectCompleted);
            ProbeSawNewGeneration.Add(round.ProbeConnectionId?.Equals(round.Before) == false);

            RegistrationSucceeded.Add((await eventStore.WaitForRegistration(TimeSpanFactory.FromSeconds(30))).IsSuccess);
            await eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>().WaitTillActive();
        }

        static async Task<(ReactorState? State, Exception? Error)> ReadState(IReactorHandler handler, Round round)
        {
            // Run off the spec's flow so a read that blocks synchronously cannot hold the round back.
            return await Task.Run<(ReactorState? State, Exception? Error)>(async () =>
            {
                var entered = false;
                try
                {
                    // GetState calls the real unary GetObserverInformation operation while Observe is subscribed.
                    var pending = handler.GetState();
                    round.PendingWhenIssued.Add(!pending.IsCompleted);
                    round.ReaderEntered();
                    entered = true;
                    return (await pending, null);
                }
                catch (Exception error)
                {
                    if (!entered)
                    {
                        round.ReaderEntered();
                    }

                    return (null, error);
                }
            });
        }

        Task OnDisconnected()
        {
            var round = _round;
            round.DisconnectObserved.TrySetResult();
            return round.ReleaseDisconnect.Task;
        }

        Task OnConnected()
        {
            var round = _round;
            if (round.ProbeConnectionId is null)
            {
                round.ProbeSawDisconnectCompleted = round.ReleaseDisconnect.Task.IsCompleted;
                round.ProbeConnectionId = round.Connection?.Lifecycle.ConnectionId;
            }

            round.NewGeneration.TrySetResult();
            return Task.CompletedTask;
        }

        sealed class Round
        {
            int _entered;

            public IChronicleConnection? Connection { get; init; }
            public ConnectionId? Before { get; init; }
            public ConnectionId? ProbeConnectionId { get; set; }
            public bool ProbeSawDisconnectCompleted { get; set; }
            public ConcurrentBag<bool> PendingWhenIssued { get; } = [];
            public TaskCompletionSource DisconnectObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource ReleaseDisconnect { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource NewGeneration { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource FenceLogged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource ReadersEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public void ReaderEntered()
            {
                if (Interlocked.Increment(ref _entered) == Readers)
                {
                    ReadersEntered.TrySetResult();
                }
            }
        }

        ChronicleClient CreateClient(ChronicleFixture configurable, ILoggerFactory loggerFactory) =>
            new(
                new ChronicleClientOptions
                {
                    ConnectionString = new ChronicleConnectionString($"chronicle://localhost:{configurable.KernelGrpcHostPort}/?skipTlsValidation=true"),
                    EventStore = EventStore.Name,
                    AutoDiscoverAndRegister = true
                },
                artifactsProvider: this,
                serviceProvider: Services,
                loggerFactory: loggerFactory);
    }

    [Fact]
    void should_complete_every_information_read()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.States.Count.ShouldEqual(context.Rounds * context.Readers);
    }

    [Fact]
    void should_not_fail_any_information_read()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.Errors.ShouldBeEmpty();
    }

    [Fact]
    void should_have_every_read_pending_when_issued()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.ReadersPending.Count.ShouldEqual(context.Rounds * context.Readers);
        Context.ReadersPending.TrueForAll(_ => _).ShouldBeTrue();
    }

    [Fact]
    void should_complete_the_disconnect_before_the_next_generation_connects()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.ProbeSawDisconnectCompleted.Count.ShouldEqual(context.Rounds);
        Context.ProbeSawDisconnectCompleted.TrueForAll(_ => _).ShouldBeTrue();
    }

    [Fact]
    void should_rotate_the_connection_id_before_the_next_generation_connects()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.ProbeSawNewGeneration.Count.ShouldEqual(context.Rounds);
        Context.ProbeSawNewGeneration.TrueForAll(_ => _).ShouldBeTrue();
    }

    [Fact]
    void should_hold_the_next_generation_back_behind_the_disconnect()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.FenceLoggedBeforeNewGeneration.TrueForAll(_ => _).ShouldBeTrue();
    }

    [Fact]
    void should_register_after_every_reconnection()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.RegistrationSucceeded.Count.ShouldEqual(context.Rounds);
        Context.RegistrationSucceeded.TrueForAll(_ => _).ShouldBeTrue();
    }

    [Fact]
    void should_return_information_for_the_registered_observer()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.States.TrueForAll(_ => _.Id == Context.FinalState.Id).ShouldBeTrue();
    }

    [Fact]
    void should_remain_active()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.FinalState.RunningState.ShouldEqual(ObserverRunningState.Active);
    }

    [Fact]
    void should_remain_subscribed()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.FinalState.IsSubscribed.ShouldBeTrue();
    }

    [Fact]
    void should_handle_events_after_reconnecting()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.FinalState.LastHandledEventSequenceNumber.ShouldEqual(Context.LastSequenceNumber);
    }

    [Fact]
    void should_deliver_events_to_the_reactor()
    {
        Assert.SkipUnless(Context.RanOutOfProcess, SkipReason);
        Context.Reactor.HandledEvents.ShouldBeGreaterThanOrEqual(2);
    }
}
