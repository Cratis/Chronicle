// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Rules.for_Rules.given;

public class all_dependencies : Specification
{
    protected IClientArtifactsProvider _clientArtifacts;
    protected IProjections _projections;
    protected JsonSerializerOptions _jsonSerializerOptions;

    void Establish()
    {
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _projections = Substitute.For<IProjections>();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
