// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

public class a_read_waiting_for_a_fold : an_immediate_projection
{
    protected ProjectionDefinition _readDefinition;
    protected ProjectionDefinition _changed;
    protected ProjectionDefinition _latest;
    protected TaskCompletionSource<SingleReadModelProjectionState> _fold;
    protected SingleReadModelProjectionState _folded;
    protected Task<ProjectionResult> _read;

    void Establish()
    {
        _readDefinition = CreateProjectionDefinition();
        _projection.GetDefinition().Returns(_readDefinition);
        var eventType = new EventType("Created", EventTypeGeneration.First);
        _projection.GetEventTypes().Returns([eventType]);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, 1)));
        var model = new ExpandoObject();
        ((IDictionary<string, object?>)model)["name"] = "folded";
        _folded = new(model, true, true);
        _fold = new TaskCompletionSource<SingleReadModelProjectionState>();
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_fold.Task, Task.FromResult(_folded));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject { ["name"] = "folded" });
        _changed = CreateProjectionDefinition() with { InitialModelState = new JsonObject { ["name"] = "changed" } };
        _latest = CreateProjectionDefinition() with { InitialModelState = new JsonObject { ["name"] = "latest" } };
        _read = _grain.GetModelInstance();
    }
}
