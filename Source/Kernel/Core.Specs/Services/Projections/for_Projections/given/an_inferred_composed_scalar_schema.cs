// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class an_inferred_composed_scalar_schema : an_inferred_read_model_preview
{
    protected const string Declaration = """
        projection PreviewScalars
          from VariantIssueOpened
            enabled = enabled
            count = count
            code = code
        """;
    protected static readonly Guid Code = Guid.Parse("11111111-2222-3333-4444-555555555555");
    protected OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;

    void Establish()
    {
        var eventSchema = JsonSchema.FromJson("""
            { "type": "object", "properties": {
                "enabled": { "anyOf": [{ "type": "boolean" }, { "type": "null" }] },
                "count": { "allOf": [{ "type": "integer", "format": "int64" }] },
                "code": { "allOf": [{ "$ref": "#/$defs/Code" }] }
              }, "$defs": { "Code": { "type": "string", "format": "guid" } } }
            """);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([new EventTypeSchema(new("VariantIssueOpened", EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, eventSchema)]);
        var projected = new ExpandoObject();
        var properties = (IDictionary<string, object?>)projected;
        properties["enabled"] = true;
        properties["count"] = 42L;
        properties["code"] = Code;
        _projectionGrain.ProcessForPreview(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ReadModelDefinition>())
            .Returns([projected]);
    }

    protected async Task Preview() => _result = await _service.Preview(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = Declaration
    });
}
