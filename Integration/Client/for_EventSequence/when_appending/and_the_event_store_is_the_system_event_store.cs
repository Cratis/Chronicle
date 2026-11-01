// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.and_the_event_store_is_the_system_event_store.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class and_the_event_store_is_the_system_event_store(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public CommandResult<AppendResponse> AppendResult;
        public CommandResult<AppendManyResponse> AppendManyResult;
        public CommandResult ReviseResult;
        public QueryResult<IEnumerable<AppendedEventResponse>> Stored;
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(SomeEvent));
            var wireEventType = new EventType { Id = eventType.Id, Generation = eventType.Generation.Value, Tombstone = eventType.Tombstone };
            var content = JsonSerializer.Serialize(new SomeEvent("some content"));
            var source = Guid.NewGuid().ToString();
            AppendResult = await services.Sequences.Append(new()
            {
                EventStore = "System", Namespace = EventStore.Namespace, EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = source, EventType = wireEventType, Content = content
            });
            AppendManyResult = await services.Sequences.AppendMany(new()
            {
                EventStore = "System", Namespace = EventStore.Namespace, EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = source, Events = [new EventToAppend { EventType = wireEventType, Content = content }]
            });
            ReviseResult = await services.Sequences.Revise(new()
            {
                EventStore = "System", Namespace = EventStore.Namespace, EventSequenceId = EventStore.EventLog.Id,
                SequenceNumber = 0, EventType = wireEventType, Content = content
            });
            Stored = await services.Sequences.QueryEvents(new()
            {
                EventStore = "System", Namespace = EventStore.Namespace, EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = source
            });
        }
    }

    [Fact] void should_refuse_append() => AssertRefused(Context.AppendResult.IsSuccess, Context.AppendResult.IsAuthorized);
    [Fact] void should_refuse_append_many() => AssertRefused(Context.AppendManyResult.IsSuccess, Context.AppendManyResult.IsAuthorized);
    [Fact] void should_refuse_revise() => AssertRefused(Context.ReviseResult.IsSuccess, Context.ReviseResult.IsAuthorized);
    [Fact] void should_not_throw_on_append() => Context.AppendResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_throw_on_append_many() => Context.AppendManyResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_throw_on_revise() => Context.ReviseResult.HasExceptions.ShouldBeFalse();
    static void AssertRefused(bool isSuccess, bool isAuthorized)
    {
        isSuccess.ShouldBeFalse();
        isAuthorized.ShouldBeFalse();
    }

    [Fact]
    void should_refuse_reading_the_system_event_store()
    {
        Context.Stored.IsSuccess.ShouldBeFalse();
        Context.Stored.IsAuthorized.ShouldBeFalse();
    }
}
