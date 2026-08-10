// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview;

public class and_the_root_is_removed : given.a_projection_grain_with_a_child_projection
{
    readonly EventType _createdEventType = new("Created", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("Removed", EventTypeGeneration.First);
    IEnumerable<ExpandoObject> _result;

    void Establish()
    {
        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType == _removedEventType)
            {
                context.Changeset.Remove();
                return;
            }

            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", null, "The model")
            ]));
        });
    }

    async Task Because() => _result = await _grain.ProcessForPreview(
        EventStoreNamespaceName.Default,
        [
            AppendedEvent.EmptyWithEventType(_createdEventType),
            AppendedEvent.EmptyWithEventType(_removedEventType)
        ],
        CreateReadModelDefinition());

    [Fact] void should_return_true_absence() => _result.ShouldBeEmpty();
    [Fact] void should_not_return_a_phantom_identifier() => _result.Cast<IDictionary<string, object?>>().SelectMany(_ => _).Any(_ => _.Key == "id").ShouldBeFalse();
    [Fact] void should_not_return_phantom_metadata() => _result.Cast<IDictionary<string, object?>>().SelectMany(_ => _).Any(_ => _.Key == WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeFalse();

    static ReadModelDefinition CreateReadModelDefinition()
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
}
