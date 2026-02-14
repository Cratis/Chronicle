// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.given;

public class a_projection_and_events_appended_to_it<TProjection, TReadModel>(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    where TProjection : class, IProjectionFor<TReadModel>, new()
    where TReadModel : class
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    protected ChronicleFixture ChronicleFixture = chronicleFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

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

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new TProjection());
    }

    void Establish()
    {
        EventSourceId = Guid.NewGuid().ToString();
        ReadModelId = EventSourceId;
    }

    async Task Because()
    {
        Projection = EventStore.Projections.GetHandlerFor<TProjection>();
        await Projection.WaitTillActive();

        AppendResult appendResult = null;
        foreach (var @event in EventsToAppend)
        {
            appendResult = await EventStore.EventLog.Append(EventSourceId, @event);
            LastEventSequenceNumber = appendResult.SequenceNumber;
        }

        foreach (var @event in EventsWithEventSourceIdToAppend)
        {
            appendResult = await EventStore.EventLog.Append(@event.EventSourceId, @event.Event);
            LastEventSequenceNumber = appendResult.SequenceNumber;
            if (WaitForEachEvent)
            {
                await WaitForProjectionAndSetResult(appendResult.SequenceNumber);
            }
        }

        var appendedEvents = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(EventSourceId, EventTypes.Select(_ => _.GetEventType()));
        AppendedEvents = [.. appendedEvents];

        if (appendResult is not null)
        {
            await WaitForProjectionAndSetResult(appendResult.SequenceNumber);
        }
    }

    protected virtual Task<TReadModel> GetReadModelResult() => GetReadModel(ReadModelId);

    protected async Task WaitForProjectionAndSetResult(EventSequenceNumber eventSequenceNumber)
    {
        await Projection.WaitTillReachesEventSequenceNumber(eventSequenceNumber);
        Result = await GetReadModelResult();
    }

    protected async Task<TReadModel> GetReadModel(EventSourceId eventSourceId)
    {
        var filter = Builders<TReadModel>.Filter.Eq(new StringFieldDefinition<TReadModel, string>("_id"), eventSourceId);
        var result = await ChronicleFixture.ReadModels.Database.GetCollection<TReadModel>().FindAsync(filter);
        return result.FirstOrDefault();
    }
}
