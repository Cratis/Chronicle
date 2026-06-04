// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.AggregateRoots;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.given;

public class a_projection_and_events_appended_to_it<TProjection, TReadModel>(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    where TProjection : class, IProjectionFor<TReadModel>, new()
    where TReadModel : class
{
    public EventSourceId EventSourceId;
    public string ReadModelId;

    public TReadModel Result;
    public AppendedEvent[] AppendedEvents;
    public EventSequenceNumber LastEventSequenceNumber = EventSequenceNumber.First;
    public override IEnumerable<Type> Projections => [typeof(TProjection)];
    protected List<object> EventsToAppend = [];
    protected List<EventAndEventSourceId> EventsWithEventSourceIdToAppend = [];
    protected IProjectionHandler Projection;
    protected bool WaitForEachEvent;

    /// <summary>
    /// Gets the bounded window the final result read polls for the instance to become visible.
    /// </summary>
    /// <remarks>
    /// The window only needs to cover the read-after-write gap between the observer reporting it
    /// reached the tail and the sink committing the instance — a short, fixed window is enough and
    /// is deliberately not the (much larger) end-to-end pipeline timeout, so scenarios that
    /// legitimately end without an instance do not stall for long.
    /// </remarks>
    protected static readonly TimeSpan ReadModelVisibilityTimeout = TimeSpanFactory.FromSeconds(30);

    /// <summary>
    /// Gets a value indicating whether the primary read model (keyed by <see cref="ReadModelId"/>)
    /// is expected to materialize once the projection has reached the tail.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>, which makes the final result read poll (within
    /// <see cref="ReadModelVisibilityTimeout"/>) until the instance is visible — closing the
    /// read-after-write gap between the observer reporting it reached the tail and the sink
    /// committing the instance. Scenarios whose instance lives under a different key space than
    /// <see cref="ReadModelId"/> (e.g. composite-key projections), or that intentionally end with
    /// the instance removed, override this to <see langword="false"/> so the read is performed once
    /// without waiting for an instance that, by design, never appears at that id.
    /// </remarks>
    protected virtual bool ExpectsPrimaryReadModel => true;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new TProjection());
    }

    void Establish()
    {
        if (EventSourceId?.IsSpecified != true)
        {
            EventSourceId = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrEmpty(ReadModelId))
        {
            ReadModelId = EventSourceId;
        }
    }

    async Task Because()
    {
        Projection = EventStore.Projections.GetHandlerFor<TProjection>();
        await Projection.WaitTillSubscribed();
        await Projection.WaitTillActive();

        IAppendResult appendResult = null;
        foreach (var @event in EventsToAppend)
        {
            var result = await EventStore.EventLog.Append(EventSourceId, @event);
            LastEventSequenceNumber = result.SequenceNumber;
            appendResult = result;
        }

        foreach (var @event in EventsWithEventSourceIdToAppend)
        {
            var result = await EventStore.EventLog.Append(@event.EventSourceId, @event.Event);
            LastEventSequenceNumber = result.SequenceNumber;
            appendResult = result;
            if (WaitForEachEvent)
            {
                await WaitForProjectionAndSetResult(result.SequenceNumber);
            }
        }

        var appendedEvents = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(EventSourceId, EventTypes.Select(_ => _.GetEventType()));
        AppendedEvents = [.. appendedEvents];

        if (appendResult is not null)
        {
            await WaitForProjectionAndSetResult(LastEventSequenceNumber, requireMaterialized: true);
        }
    }

    protected virtual Task<TReadModel> GetReadModelResult() => GetReadModel(ReadModelId);

    /// <summary>
    /// Waits for the projection to reach the given sequence number and reads the resulting instance into <see cref="Result"/>.
    /// </summary>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> the projection observer must reach first.</param>
    /// <param name="requireMaterialized">
    /// When <see langword="true"/> (the final read after all events are appended), the read is retried
    /// within <see cref="ReadModelVisibilityTimeout"/> until the instance is visible — closing the
    /// read-after-write gap that otherwise leaves <see cref="Result"/> null and surfaces as a confusing
    /// <see cref="NullReferenceException"/> in a downstream assertion. The wait is best-effort: if the
    /// instance never appears (a scenario may legitimately end with it removed) the read simply returns
    /// null. When <see langword="false"/> (per-event intermediate reads), the instance may legitimately
    /// not exist yet, so it is read once without waiting.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task WaitForProjectionAndSetResult(EventSequenceNumber eventSequenceNumber, bool requireMaterialized = false)
    {
        await Projection.WaitTillReachesEventSequenceNumber(eventSequenceNumber);

        if (!requireMaterialized || !ExpectsPrimaryReadModel)
        {
            Result = await GetReadModelResult();
            return;
        }

        var deadline = DateTime.UtcNow.Add(ReadModelVisibilityTimeout);
        while (true)
        {
            Result = await GetReadModelResult();
            if (Result is not null || DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(50);
        }
    }

    protected async Task<TReadModel> GetReadModel(EventSourceId eventSourceId) =>
        await EventStore.ReadModels.GetInstanceById<TReadModel>(eventSourceId);
}
