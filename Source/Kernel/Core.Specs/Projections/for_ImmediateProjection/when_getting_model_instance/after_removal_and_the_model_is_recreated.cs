// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class after_removal_and_the_model_is_recreated : given.an_immediate_projection
{
    readonly EventType _createdEventType = new("ModelCreated", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("ModelRemoved", EventTypeGeneration.First);
    readonly EventType _recreatedEventType = new("ModelRecreated", EventTypeGeneration.First);
    ProjectionResult _afterRemoval;
    ProjectionResult _firstRepeatedAbsence;
    ProjectionResult _secondRepeatedAbsence;
    ProjectionResult _afterRecreation;
    SingleReadModelProjectionState _recreationInitialState;
    int _processingCalls;

    void Establish()
    {
        var created = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_createdEventType, 1);
        var removed = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_removedEventType, 2);
        var recreated = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_recreatedEventType, 3);
        var recreationCursor = CreateCursor(created, removed);
        var page = -1;
        recreationCursor.Current.Returns(_ => page == 0 ? [created, removed] : [recreated]);
        recreationCursor.MoveNext().Returns(_ => ++page < 2);
        var cursors = new Queue<Storage.EventSequences.IEventCursor>([
            CreateCursor(created, removed),
            CreateCursor(created, removed),
            CreateCursor(created, removed),
            recreationCursor]);

        _projection.GetEventTypes().Returns([_createdEventType, _removedEventType, _recreatedEventType]);
        _eventSequenceStorage
            .GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => cursors.Dequeue());
        _projection
            .FoldForSingleReadModel(
                EventStoreNamespaceName.Default,
                Arg.Any<SingleReadModelProjectionState>(),
                Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _processingCalls++;
                var state = new ExpandoObject();
                if (events[^1].Context.EventType == _recreatedEventType)
                {
                    _recreationInitialState = call.Arg<SingleReadModelProjectionState>();
                    ((IDictionary<string, object?>)state)["name"] = "Recreated";
                }

                return Task.FromResult(new SingleReadModelProjectionState(state, events[^1].Context.EventType == _recreatedEventType, events[^1].Context.EventType == _recreatedEventType));
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
        _firstRepeatedAbsence = await _grain.GetModelInstance();
        _secondRepeatedAbsence = await _grain.GetModelInstance();
        _afterRecreation = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_removal_as_absent() => _afterRemoval.HasReadModel.ShouldBeFalse();
    [Fact] void should_count_the_events_that_produced_the_removal() => _afterRemoval.ProjectedEventsCount.ShouldEqual(2);
    [Fact] void should_remember_the_removal_tail() => _afterRemoval.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_keep_the_first_repeated_read_absent() => _firstRepeatedAbsence.HasReadModel.ShouldBeFalse();
    [Fact] void should_refold_the_events_for_the_first_repeated_read() => _firstRepeatedAbsence.ProjectedEventsCount.ShouldEqual(2);
    [Fact] void should_keep_the_second_repeated_read_absent() => _secondRepeatedAbsence.HasReadModel.ShouldBeFalse();
    [Fact] void should_refold_the_events_for_the_second_repeated_read() => _secondRepeatedAbsence.ProjectedEventsCount.ShouldEqual(2);
    [Fact] void should_report_the_recreated_model_as_present() => _afterRecreation.HasReadModel.ShouldBeTrue();
    [Fact] void should_process_the_full_history_including_recreation() => _afterRecreation.ProjectedEventsCount.ShouldEqual(3);
    [Fact] void should_advance_to_the_recreation_tail() => _afterRecreation.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_process_every_reads_batches() => _processingCalls.ShouldEqual(5);
    [Fact] void should_recreate_from_an_empty_state() => _recreationInitialState.Model.ShouldBeEmpty();
    [Fact] void should_carry_absence_into_the_recreation_batch() => _recreationInitialState.Exists.ShouldBeFalse();
    [Fact] void should_carry_the_uninitialized_root_into_the_recreation_batch() => _recreationInitialState.IsInitialized.ShouldBeFalse();
    [Fact] void should_contain_the_recreated_state() => _afterRecreation.ReadModel["name"]!.GetValue<string>().ShouldEqual("Recreated");
    [Fact] void should_not_contain_state_from_before_the_removal() => _afterRecreation.ReadModel.ContainsKey("oldName").ShouldBeFalse();
}
