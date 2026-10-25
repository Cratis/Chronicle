// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A global execution snapshot cannot reserve a private generation.</summary>
public class when_the_projection_is_global : given.a_generation_build_owner
{
    /// <summary>Supplies a digest-valid global snapshot, which Capture deliberately refuses to create.</summary>
    void Establish()
    {
        var content = JsonNode.Parse(_request.Execution.CanonicalContent)!.AsObject();
        var projection = content.Single(_ => string.Equals(_.Key, "projection", StringComparison.OrdinalIgnoreCase)).Value!.AsObject();
        var scope = projection.FirstOrDefault(_ => string.Equals(_.Key, "scope", StringComparison.OrdinalIgnoreCase)).Key ?? "scope";
        projection[scope] = nameof(ProjectionScope.Global);
        var execution = MaterializationExecutionSpecification.Capture(_request.Execution.EventSequence, GenerationExecutionSpecifications.Canonicalize(content.ToJsonString()));
        _request = _request with { Execution = execution };
    }

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The owner refuses global execution inputs.</summary>
    [Fact] void should_be_unsupported() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Unsupported);

    /// <summary>No candidate is reserved.</summary>
    [Fact] async Task should_leave_only_the_serving_generation() => (await Binding()).Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
