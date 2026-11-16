// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.AggregateRoots;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join.given;

public class a_watched_users_in_groups<TProjection>(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    where TProjection : class, IProjectionFor<WatchedUser>, new()
{
    protected static readonly TimeSpan _timeout = TimeSpanFactory.FromSeconds(30);

    public readonly ConcurrentQueue<ReadModelChangeset<WatchedUser>> Changesets = new();
    protected IProjectionHandler _projection;
#pragma warning disable CA2213 // Disposed in Destroy, which the analyzer does not see as a Dispose method.
    readonly SemaphoreSlim _arrived = new(0);
    IDisposable _subscription;
#pragma warning restore CA2213

    public override IEnumerable<Type> EventTypes => [typeof(GroupCreated), typeof(UserCreated), typeof(UserAddedToGroup), typeof(GroupRemoved), typeof(UserRemoved)];
    public override IEnumerable<Type> Projections => [typeof(TProjection)];

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new TProjection());

    async Task Establish()
    {
        _subscription = EventStore.ReadModels.Watch<WatchedUser>().Subscribe(changeset =>
        {
            Changesets.Enqueue(changeset);
            _arrived.Release();
        });

        _projection = EventStore.Projections.GetHandlerFor<TProjection>();
        await _projection.WaitTillSubscribed();
        await _projection.WaitTillActive();
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _arrived.Dispose();
    }

    /// <summary>
    /// Appends the setup events and waits until the watcher has seen every change they caused, so the projection is known to be
    /// watched and no setup change can arrive after the setup has been cleared.
    /// </summary>
    /// <remarks>
    /// Every setup event must be handled by the projection. Each partition is handled by its own subscriber, so the changesets of
    /// different partitions reach the watcher independently and in any order. Waiting only for the first changeset of one user let
    /// the remaining setup changesets arrive after <see cref="Changesets"/> was cleared, where they were counted as changes caused
    /// by the removal. A changeset carries the sequence number of the last event of its run, so waiting for the last setup event
    /// of every partition waits for all of them, however the events were batched.
    /// </remarks>
    /// <param name="setup">The events to append, each with its event source.</param>
    /// <returns>An awaitable task.</returns>
    protected async Task AppendSetup(params EventAndEventSourceId[] setup)
    {
        var lastByPartition = new Dictionary<string, EventSequenceNumber>();
        foreach (var @event in setup)
        {
            var result = await EventStore.EventLog.Append(@event.EventSourceId, @event.Event);
            lastByPartition[@event.EventSourceId.Value] = result.SequenceNumber;
        }

        foreach (var sequenceNumber in lastByPartition.Values)
        {
            await WaitFor(changeset => changeset.ChangeContext?.SequenceNumber == sequenceNumber);
        }

        Changesets.Clear();
    }

    /// <summary>
    /// Appends an event and waits for the projection to handle it.
    /// </summary>
    /// <param name="eventSourceId">The event source to append for.</param>
    /// <param name="event">The event.</param>
    /// <returns>An awaitable task.</returns>
    protected async Task AppendAndHandle(EventSourceId eventSourceId, object @event)
    {
        var result = await EventStore.EventLog.Append(eventSourceId, @event);
        await _projection.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
    }

    /// <summary>
    /// Waits until a changeset matching the predicate has been received.
    /// </summary>
    /// <param name="predicate">What the changeset must satisfy.</param>
    /// <returns>An awaitable task.</returns>
    protected async Task WaitFor(Func<ReadModelChangeset<WatchedUser>, bool> predicate)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (!Changesets.Any(predicate))
        {
            var remaining = deadline - DateTime.UtcNow;
            (remaining > TimeSpan.Zero && await _arrived.WaitAsync(remaining)).ShouldBeTrue();
        }
    }

    /// <summary>
    /// Waits until the given number of changesets has been received.
    /// </summary>
    /// <param name="count">The number of changesets.</param>
    /// <returns>An awaitable task.</returns>
    protected async Task WaitForCount(int count)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (Changesets.Count < count)
        {
            var remaining = deadline - DateTime.UtcNow;
            (remaining > TimeSpan.Zero && await _arrived.WaitAsync(remaining)).ShouldBeTrue();
        }
    }
}
