// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_root_is_uninitialized : given.an_immediate_projection
{
    ProjectionResult _result;
    ProjectionResult _cached;

    void Establish()
    {
        var joined = new EventType("Joined", EventTypeGeneration.First);
        _projection.GetEventTypes().Returns([joined]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)1);
        var cursor = CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(joined, 1));
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        var model = new ExpandoObject();
        ((IDictionary<string, object?>)model)["name"] = "Joined before creation";
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(new SingleReadModelProjectionState(model, false, true));
    }

    async Task Because()
    {
        _result = await _grain.GetModelInstance();
        _cached = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_placeholder_as_absent() => _result.HasReadModel.ShouldBeFalse();
    [Fact] void should_keep_the_cached_placeholder_absent() => _cached.HasReadModel.ShouldBeFalse();
}
