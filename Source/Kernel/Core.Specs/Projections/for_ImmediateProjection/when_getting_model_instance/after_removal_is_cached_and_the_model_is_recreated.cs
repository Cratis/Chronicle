// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class after_removal_is_cached_and_the_model_is_recreated : given.an_immediate_projection
{
    readonly EventType _createdEventType = new("ModelCreated", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("ModelRemoved", EventTypeGeneration.First);
    readonly EventType _recreatedEventType = new("ModelRecreated", EventTypeGeneration.First);
    ProjectionResult _afterRemoval;
    ProjectionResult _firstCachedAbsence;
    ProjectionResult _secondCachedAbsence;
    ProjectionResult _afterRecreation;
    ExpandoObject _recreationInitialState;
    int _processingCalls;

    void Establish()
    {
        var created = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_createdEventType, 1);
        var removed = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_removedEventType, 2);
        var recreated = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_recreatedEventType, 3);
        var tails = new Queue<EventSequenceNumber>([(EventSequenceNumber)2, (EventSequenceNumber)2, (EventSequenceNumber)2, (EventSequenceNumber)3]);
        var createAndRemoveCursor = CreateCursor(created, removed);
        var recreateCursor = CreateCursor(recreated);

        _projection.GetEventTypes().Returns([_createdEventType, _removedEventType, _recreatedEventType]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => Task.FromResult(tails.Dequeue()));
        _eventSequenceStorage
            .GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(createAndRemoveCursor);
        _eventSequenceStorage
            .GetFromSequenceNumber((EventSequenceNumber)3, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(recreateCursor);
        _projection
            .ProcessForSingleReadModel(
                EventStoreNamespaceName.Default,
                Arg.Any<ExpandoObject>(),
                Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _processingCalls++;
                var state = new ExpandoObject();
                if (events[^1].Context.EventType == _recreatedEventType)
                {
                    _recreationInitialState = call.Arg<ExpandoObject>();
                    ((IDictionary<string, object?>)state)["name"] = "Recreated";
                }

                return Task.FromResult(state);
            });
        _expandoObjectConverter
            .ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call =>
            {
                var json = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    json[key] = value?.ToString();
                }

                return json;
            });
    }

    async Task Because()
    {
        _afterRemoval = await _grain.GetModelInstance();
        _firstCachedAbsence = await _grain.GetModelInstance();
        _secondCachedAbsence = await _grain.GetModelInstance();
        _afterRecreation = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_removal_as_absent() => _afterRemoval.HasReadModel.ShouldBeFalse();
    [Fact] void should_count_the_events_that_produced_the_removal() => _afterRemoval.ProjectedEventsCount.ShouldEqual(2);
    [Fact] void should_remember_the_removal_tail() => _afterRemoval.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_keep_the_first_cached_read_absent() => _firstCachedAbsence.HasReadModel.ShouldBeFalse();
    [Fact] void should_not_reprocess_events_for_the_first_cached_read() => _firstCachedAbsence.ProjectedEventsCount.ShouldEqual(0);
    [Fact] void should_keep_the_second_cached_read_absent() => _secondCachedAbsence.HasReadModel.ShouldBeFalse();
    [Fact] void should_not_reprocess_events_for_the_second_cached_read() => _secondCachedAbsence.ProjectedEventsCount.ShouldEqual(0);
    [Fact] void should_report_the_recreated_model_as_present() => _afterRecreation.HasReadModel.ShouldBeTrue();
    [Fact] void should_process_only_the_recreation_event() => _afterRecreation.ProjectedEventsCount.ShouldEqual(1);
    [Fact] void should_advance_to_the_recreation_tail() => _afterRecreation.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_not_process_events_for_either_cached_read() => _processingCalls.ShouldEqual(2);
    [Fact] void should_recreate_from_an_empty_state() => _recreationInitialState.ShouldBeEmpty();
    [Fact] void should_contain_the_recreated_state() => _afterRecreation.ReadModel["name"]!.GetValue<string>().ShouldEqual("Recreated");
    [Fact] void should_not_contain_state_from_before_the_removal() => _afterRecreation.ReadModel.ContainsKey("oldName").ShouldBeFalse();
}
