// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Aksio.Cratis.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IRulesProjections> rules_projections;
    protected Mock<IImmediateProjections> immediate_projections;
    protected JsonSerializerOptions json_serializer_options;
    protected ExecutionContext execution_context;

    void Establish()
    {
        client_artifacts = new();
        rules_projections = new();
        immediate_projections = new();
        json_serializer_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        execution_context = new(
            "1ca3e772-4e18-47b1-8f7b-c697b2c8ee8f",
            TenantId.Development,
            CorrelationId.New());
    }
}
