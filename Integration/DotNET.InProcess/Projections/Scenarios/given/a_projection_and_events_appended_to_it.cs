// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.given;


public class a_projection_and_events_appended_to_it<TProjection, TModel>(ChronicleFixture ChronicleFixture) : IntegrationSpecificationContext(ChronicleFixture)
    where TProjection : class, IProjectionFor<TModel>, new()
    where TModel : class
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    protected ChronicleFixture _ChronicleFixture = ChronicleFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public EventSourceId EventSourceId;
    public string ModelId;

    public TModel Result;
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
        ModelId = EventSourceId;
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

    protected virtual Task<TModel> GetModelResult() => GetModel(ModelId);

    protected async Task WaitForProjectionAndSetResult(EventSequenceNumber eventSequenceNumber)
    {
        await Projection.WaitTillReachesEventSequenceNumber(eventSequenceNumber);
        Result = await GetModelResult();
    }

    protected async Task<TModel> GetModel(EventSourceId eventSourceId)
    {
        var filter = Builders<TModel>.Filter.Eq(new StringFieldDefinition<TModel, string>("_id"), eventSourceId);
        var result = await _ChronicleFixture.ReadModels.Database.GetCollection<TModel>().FindAsync(filter);
        return result.FirstOrDefault();
    }
}
