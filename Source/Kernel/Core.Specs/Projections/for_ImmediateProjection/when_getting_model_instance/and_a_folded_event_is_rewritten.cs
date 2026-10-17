// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_a_folded_event_is_rewritten : given.an_immediate_projection
{
    readonly EventType _eventType = new("ModelNamed", EventTypeGeneration.First);
    AppendedEvent _storedEvent;
    ProjectionResult _before;
    ProjectionResult _after;
    SingleReadModelProjectionState _secondInitialState;
    int _folds;

    void Establish()
    {
        _storedEvent = Named("Original");
        _projection.GetEventTypes().Returns([_eventType]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)5);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor(_storedEvent));
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                if (++_folds == 2)
                {
                    _secondInitialState = call.Arg<SingleReadModelProjectionState>();
                }

                return new SingleReadModelProjectionState(call.Arg<IEnumerable<AppendedEvent>>().Single().Content, true, true);
            });
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => new JsonObject { ["name"] = (string)((IDictionary<string, object?>)call.Arg<ExpandoObject>())["name"]! });
    }

    async Task Because()
    {
        _before = await _grain.GetModelInstance();
        _storedEvent = Named("Rewritten");
        _after = await _grain.GetModelInstance();
    }

    [Fact] void should_read_the_original_content_first() => _before.ReadModel["name"]!.GetValue<string>().ShouldEqual("Original");
    [Fact] void should_read_the_rewritten_content_without_a_new_append() => _after.ReadModel["name"]!.GetValue<string>().ShouldEqual("Rewritten");
    [Fact] void should_start_the_second_fold_without_the_previous_model() => _secondInitialState.Model.ShouldBeEmpty();
    [Fact] void should_report_the_same_last_matching_sequence_number() => _after.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_report_the_full_fold_count_on_the_second_read() => _after.ProjectedEventsCount.ShouldEqual(1);

    AppendedEvent Named(string name)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["name"] = name;
        return AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 5) with { Content = content };
    }
}
