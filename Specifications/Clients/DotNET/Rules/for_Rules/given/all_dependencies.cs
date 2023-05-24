// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Execution;
using Aksio.Models;
using Aksio.Cratis.Schemas;
using Aksio.Types;
using NJsonSchema;

namespace Aksio.Cratis.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected Mock<IEventTypes> event_types;
    protected Mock<IModelNameConvention> model_name_convention;
    protected Mock<IJsonSchemaGenerator> json_schema_generator;
    protected Mock<ITypes> types;
    protected Mock<IImmediateProjections> immediate_projections;
    protected JsonSerializerOptions json_serializer_options;
    protected ExecutionContext execution_context;

    void Establish()
    {
        event_types = new();
        model_name_convention = new();
        types = new();
        immediate_projections = new();
        json_schema_generator = new();
        json_schema_generator.Setup(_ => _.Generate(IsAny<Type>())).Returns(new JsonSchema());
        json_serializer_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        event_types.Setup(_ => _.HasFor(typeof(SomeEvent))).Returns(true);
        event_types.Setup(_ => _.GetEventTypeFor(typeof(SomeEvent))).Returns(new EventType(SomeEvent.EventTypeId,1));

        execution_context = new(
            "1ca3e772-4e18-47b1-8f7b-c697b2c8ee8f",
            TenantId.Development,
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System);
    }
}
