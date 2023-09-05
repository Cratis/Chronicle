// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Models;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Projections.for_Projections.given;

public class all_dependencies : Specification
{
    protected Mock<IEventTypes> event_types;
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IJsonSchemaGenerator> schema_generator;
    protected Mock<IModelNameResolver> model_name_resolver;
    protected Mock<IServiceProvider> service_provider;
    protected JsonSerializerOptions json_serializer_options;

    void Establish()
    {
        event_types = new();
        client_artifacts = new();
        schema_generator = new();
        model_name_resolver = new();
        service_provider = new();
        json_serializer_options = new();
    }
}
