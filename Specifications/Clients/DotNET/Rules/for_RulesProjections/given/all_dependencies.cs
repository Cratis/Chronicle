// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Models;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Rules.for_RulesProjections.given;

public class all_dependencies : Specification
{
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IEventTypes> event_types;
    protected Mock<IModelNameResolver> model_name_resolver;
    protected Mock<IJsonSchemaGenerator> json_schema_generator;
    protected JsonSerializerOptions serializer_options;

    void Establish()
    {
        service_provider = new();
        client_artifacts = new();
        event_types = new();
        model_name_resolver = new();
        json_schema_generator = new();
        serializer_options = new();
    }
}
