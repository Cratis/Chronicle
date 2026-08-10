// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_sequence_ending_after_removal : a_projection_grain_with_a_child_projection
{
    readonly EventType _createdEventType = new("Created", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("Removed", EventTypeGeneration.First);
    readonly EventType _rootJoinEventType = new("RootJoin", EventTypeGeneration.First);
    readonly EventType _noOpEventType = new("NoOp", EventTypeGeneration.First);
    readonly EventType _combinedFromJoinEventType = new("CombinedFromJoin", EventTypeGeneration.First);

    protected AppendedEvent CreatedEvent => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_createdEventType, 1);

    protected AppendedEvent RemovedEvent => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_removedEventType, 2);

    protected AppendedEvent RootJoinEvent => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_rootJoinEventType, 3);

    protected AppendedEvent NoOpEvent => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_noOpEventType, 3);

    protected AppendedEvent CombinedFromJoinEvent => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_combinedFromJoinEventType, 3);

    protected void ConfigureRootJoinAfterRemoval() => ConfigureTrailingEvent(_rootJoinEventType, true, ProjectionOperationType.Join);

    protected void ConfigureNoOpAfterRemoval() => ConfigureTrailingEvent(_noOpEventType, false, ProjectionOperationType.None);

    protected void ConfigureCombinedFromJoinAfterRemoval()
    {
        ConfigureTrailingEvent(_combinedFromJoinEventType, true, ProjectionOperationType.From | ProjectionOperationType.Join);
        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType != _combinedFromJoinEventType)
            {
                return;
            }

            var joinedState = new ExpandoObject();
            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", null, "The recreated model")
            ]));
            context.Changeset.Add(new Joined(
                joinedState,
                "the-join-key",
                new PropertyPath("CustomerId"),
                ArrayIndexers.NoIndexers,
                [
                    new PropertiesChanged<ExpandoObject>(joinedState,
                    [
                        new PropertyDifference("CustomerName", null, "Ada")
                    ])
                ]));
        });
    }

    protected static ReadModelDefinition CreatePreviewReadModelDefinition()
    {
        var schema = new JsonSchema();
        schema.Properties["id"] = new JsonSchemaProperty("id", new JsonObject { ["type"] = "string" }, schema);
        schema.Properties["Name"] = new JsonSchemaProperty("Name", new JsonObject { ["type"] = "string" }, schema);

        return new(
            "the-read-model",
            "the-read-models",
            "The read model",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "the-projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
    }

    void ConfigureTrailingEvent(EventType trailingEventType, bool acceptsTrailingEvent, ProjectionOperationType trailingOperationType)
    {
        _rootProjection.Accepts(Arg.Any<EventType>()).Returns(call => call.Arg<EventType>() != trailingEventType || acceptsTrailingEvent);
        _rootProjection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(call => call.Arg<EventType>() == trailingEventType ? trailingOperationType : ProjectionOperationType.From);

        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType == _removedEventType)
            {
                context.Changeset.Remove();
                return;
            }

            if (context.Event.Context.EventType != _createdEventType)
            {
                return;
            }

            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", null, "The model")
            ]));
        });
    }
}
