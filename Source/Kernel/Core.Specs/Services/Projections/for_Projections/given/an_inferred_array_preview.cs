// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class an_inferred_array_preview : an_inferred_read_model_preview
{
    protected JsonSchema _arrayEventSchema;
    protected OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;
    protected Exception _error;

    void Establish()
    {
        _arrayEventSchema = JsonSchema.FromJson("""
            { "type": "object", "properties": {
              "items": { "type": "array", "minItems": 1, "items": {
                "type": "object", "properties": {
                  "name": { "type": "string" },
                  "quantity": { "type": "integer", "format": "int32", "minimum": 1 }
                }, "required": ["name"], "default": { "name": "Default", "$ref": "literal" }
              } }
            } }
            """);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns(_ => (EventTypeSchema[])[new(new("VariantIssueOpened", EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, _arrayEventSchema)]);
        _projectionGrain.ProcessForPreview(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ReadModelDefinition>())
            .Returns(call =>
            {
                var child = new ExpandoObject();
                ((IDictionary<string, object?>)child)["name"] = "Book";
                ((IDictionary<string, object?>)child)["quantity"] = "3";
                var content = new ExpandoObject();
                ((IDictionary<string, object?>)content)["items"] = new List<ExpandoObject> { child };
                var appended = new AppendedEvent(EventContext.EmptyWithEventSourceId("source"), content);
                var resolvers = new EventValueProviderExpressionResolvers(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);
                var value = resolvers.Resolve(call.Arg<ReadModelDefinition>().GetSchemaForLatestGeneration().Properties["items"], "items")(appended);
                var projected = new ExpandoObject();
                ((IDictionary<string, object?>)projected)["items"] = value;
                return (ExpandoObject[])[projected];
            });
    }

    protected async Task Preview() => _result = await _service.Preview(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = """
            projection PreviewItems
              from VariantIssueOpened
                items = items
            """
    });

    protected JsonSchema InferredSchema() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema);

    protected JsonObject PreviewedChild() => JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["items"]![0]!.AsObject();
}
