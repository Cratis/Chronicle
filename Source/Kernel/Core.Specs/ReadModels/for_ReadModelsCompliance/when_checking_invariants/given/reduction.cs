// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class reduction
{
    public static async Task<ExpandoObject> Store(IReadModelsCompliance compliance, JsonSchema schema, ExpandoObject state, string subject)
    {
        var readModel = new ReadModelDefinition("union", "Union", "Union", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Reducer, "observer", new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB), new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)1] = schema }, []);
        var sink = Substitute.For<ISink>();
        sink.FindOrDefault(Arg.Any<Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
        sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(Task.FromResult(Enumerable.Empty<FailedPartition>()));
        ExpandoObject? stored = null;
        var observed = Substitute.For<IReadModelsCompliance>();
        observed.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(async call => stored = await compliance.Apply(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<ExpandoObject>(4)));
        var pipeline = new ReducerPipeline(readModel, sink, new ObjectComparer(), observed, "store", "Default");
        var @event = new AppendedEvent(EventContext.From("store", "Default", EventType.Unknown, EventSourceType.Default, subject, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet), new ExpandoObject());
        await pipeline.Reduce(new ReducerContext([@event], new Key(subject, ArrayIndexers.NoIndexers)), (_, _) => Task.FromResult(new ReducerSubscriberResult(new ObserverSubscriberResult(ObserverSubscriberState.Ok, EventSequenceNumber.First, [], string.Empty), state)));
        await sink.Received(1).ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
        return stored!;
    }
}
