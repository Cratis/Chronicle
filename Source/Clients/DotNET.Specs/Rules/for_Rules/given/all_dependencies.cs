// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IRulesProjections> rules_projections;
    protected Mock<IProjections> projections;
    protected JsonSerializerOptions json_serializer_options;

    void Establish()
    {
        client_artifacts = new();
        rules_projections = new();
        projections = new();
        json_serializer_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
