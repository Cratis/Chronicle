// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Clustering.for_ManyClientObservers;

/// <summary>
/// Registers many runtime reactors on one client connection, appends events and records what each received.
/// </summary>
/// <remarks>
/// The scenario is the same whatever carries the observation to the kernel (one stream per reactor or one stream
/// for the whole connection); the specs that run it pin that the carrier makes no difference to delivery.
/// </remarks>
public class ManyObserversScenario
{
    public const int Reactors = 12;
    public const int Events = 6;

    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);
    readonly ConcurrentDictionary<int, ConcurrentQueue<ulong>> _received = new();

    /// <summary>
    /// Gets what each reactor received, in the order it received it, keyed by reactor index.
    /// </summary>
    public IReadOnlyDictionary<int, ulong[]> Received => _received.ToDictionary(_ => _.Key, _ => _.Value.ToArray());

    /// <summary>
    /// Gets the sequence numbers that were appended, in order.
    /// </summary>
    public IReadOnlyList<ulong> Appended { get; private set; } = [];

    /// <summary>
    /// Runs the scenario against the given event store.
    /// </summary>
    /// <param name="eventStore">The client event store to register the reactors on.</param>
    /// <param name="prefix">A prefix that keeps this run's reactor identifiers apart from other runs.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a reactor does not receive every event in time.</exception>
    public async Task Run(IEventStore eventStore, string prefix)
    {
        var eventType = eventStore.EventTypes.GetEventTypeFor(typeof(ManyObserversEvent));
        var handlers = new List<IReactorHandler>();
        for (var index = 0; index < Reactors; index++)
        {
            var captured = index;
            _received[captured] = new();
            handlers.Add(await eventStore.Reactors.Register(
                $"{prefix}-{index}",
                builder => builder.WithEventType(eventType),
                (@event, _) =>
                {
                    _received[captured].Enqueue(@event.Context.SequenceNumber.Value);
                    return Task.CompletedTask;
                }));
        }

        foreach (var handler in handlers)
        {
            await handler.WaitTillSubscribed(_timeout);
        }

        var appended = new List<ulong>();
        var source = $"{prefix}-source";
        for (var sequence = 1; sequence <= Events; sequence++)
        {
            var result = await eventStore.EventLog.Append(source, new ManyObserversEvent(sequence));
            appended.Add(result.SequenceNumber.Value);
        }

        Appended = appended;
        using var cancellation = new CancellationTokenSource(_timeout);
        while (_received.Values.Any(_ => _.Count < Events))
        {
            if (cancellation.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Not every reactor received {Events} events: {string.Join(", ", _received.OrderBy(_ => _.Key).Select(_ => $"{_.Key}={_.Value.Count}"))}.");
            }

            // Task.Delay: poll interval while waiting for every reactor to receive its events, bounded by the cancellation.
            await Task.Delay(100, cancellation.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
