// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

public class a_language_service_with_qualified_read_models : a_language_service
{
    protected const string ReadModelName = "Example.ReadModels.Order";
    protected List<ReadModelDefinition> _readModels;
    protected EventTypeSchema[] _eventTypes;
    protected CompilerErrors _errors;

    void Establish()
    {
        _readModels = [CreateReadModel(ReadModelName)];
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        _eventTypes = [new EventTypeSchema((EventType)"OrderPlaced", EventTypeOwner.Client, EventTypeSource.Code, schema)];
    }

    protected static ReadModelDefinition CreateReadModel(string identifier, string title = "OrderSummary")
    {
        var schema = new JsonSchema { Title = title, Type = JsonObjectType.Object };
        schema.Properties["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["IsActive"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean };
        schema.Properties["Occurred"] = new JsonSchemaProperty { Type = JsonObjectType.String, Format = "date-time" };
        var details = new JsonSchemaProperty { Type = JsonObjectType.Object };
        details.Properties["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["Details"] = details;
        schema.Properties["Items"] = new JsonSchemaProperty { Type = JsonObjectType.Array, Item = details };
        return new ReadModelDefinition(
            new ReadModelIdentifier(identifier),
            new ReadModelContainerName("orders"),
            new ReadModelDisplayName(title),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
    }

    protected void CompileExpectingErrors(string declaration) => _errors = _languageService.Compile(
        declaration, ProjectionOwner.Client, _readModels, _eventTypes).Match(_ => CompilerErrors.Empty, errors => errors);
}
