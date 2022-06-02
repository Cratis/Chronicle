// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using NJsonSchema;
using Orleans;

namespace Aksio.Cratis.Applications.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected Mock<IEventTypes> event_types;
    protected Mock<IJsonSchemaGenerator> json_schema_generator;
    protected Mock<ITypes> types;
    protected Mock<IClusterClient> cluster_client;
    protected JsonSerializerOptions json_serializer_options;
    protected ExecutionContext execution_context;

    void Establish()
    {
        event_types = new();
        types = new();
        cluster_client = new();
        json_schema_generator = new();
        json_schema_generator.Setup(_ => _.Generate(IsAny<Type>())).Returns(new JsonSchema());
        json_serializer_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        execution_context = new(
            "1ca3e772-4e18-47b1-8f7b-c697b2c8ee8f",
            TenantId.Development,
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System);
    }
}
