// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class all_dependencies : Specification
{
    protected Mock<IEventStore> event_store;
    protected Mock<IEventTypes> event_types;
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IRulesProjections> rules_projections;
    protected Mock<IJsonSchemaGenerator> schema_generator;
    protected Mock<IModelNameResolver> model_name_resolver;
    protected Mock<IEventSerializer> event_serializer;
    protected Mock<IServiceProvider> service_provider;
    protected JsonSerializerOptions json_serializer_options;

    void Establish()
    {
        event_store = new();
        event_types = new();
        client_artifacts = new();
        rules_projections = new();
        schema_generator = new();
        model_name_resolver = new();
        event_serializer = new();
        service_provider = new();
        json_serializer_options = new();
    }
}
