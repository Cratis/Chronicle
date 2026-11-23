// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation.when_a_reactor_is_slow_on_a_shared_stream.context;

namespace Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation;

/// <summary>
/// A reactor that holds an event does not stop another reactor on the same stream from receiving its events.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(MultiplexedChronicleCollection.Name)]
public class when_a_reactor_is_slow_on_a_shared_stream(context _context)
    : IClassFixture<context>
{
    public class context(MultiplexedClusteringFixture fixture) : IAsyncLifetime
    {
        public const int Events = 5;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly ConcurrentQueue<ulong> _slow = new();
        readonly ConcurrentQueue<ulong> _quick = new();

        public int QuickReceivedWhileSlowWasHeld { get; private set; }

        public IReadOnlyList<ulong> SlowReceived => [.. _slow];

        public IReadOnlyList<ulong> Appended { get; private set; } = [];

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            var eventType = eventStore.EventTypes.GetEventTypeFor(typeof(SlowObserverEvent));
            var slow = await eventStore.Reactors.Register(
                "multiplexed-slow",
                builder => builder.WithEventType(eventType),
                async (@event, _) =>
                {
                    await _release.Task;
                    _slow.Enqueue(@event.Context.SequenceNumber.Value);
                });
            var quick = await eventStore.Reactors.Register(
                "multiplexed-quick",
                builder => builder.WithEventType(eventType),
                (@event, _) =>
                {
                    _quick.Enqueue(@event.Context.SequenceNumber.Value);
                    return Task.CompletedTask;
                });
            await slow.WaitTillSubscribed(_timeout);
            await quick.WaitTillSubscribed(_timeout);

            var appended = new List<ulong>();
            for (var sequence = 1; sequence <= Events; sequence++)
            {
                appended.Add((await eventStore.EventLog.Append("multiplexed-slow-source", new SlowObserverEvent(sequence))).SequenceNumber.Value);
            }

            Appended = appended;

            using var deadline = new CancellationTokenSource(_timeout);
            while (_quick.Count < Events && !deadline.IsCancellationRequested)
            {
                // Task.Delay: poll interval while waiting for the quick reactor, bounded by the deadline.
                await Task.Delay(50, deadline.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            QuickReceivedWhileSlowWasHeld = _quick.Count;
            _release.SetResult();
            while (_slow.Count < Events && !deadline.IsCancellationRequested)
            {
                // Task.Delay: poll interval while waiting for the released slow reactor, bounded by the deadline.
                await Task.Delay(50, deadline.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_to_the_quick_reactor_while_the_slow_one_holds_an_event() => _context.QuickReceivedWhileSlowWasHeld.ShouldEqual(context.Events);
    [Fact] void should_deliver_every_event_to_the_slow_reactor_in_order_once_it_catches_up() => _context.SlowReceived.ShouldEqual(_context.Appended);
}
