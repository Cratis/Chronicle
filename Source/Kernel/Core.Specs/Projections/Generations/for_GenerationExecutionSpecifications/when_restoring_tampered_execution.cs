// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationExecutionSpecifications;

public class when_restoring_tampered_execution : Specification
{
    GenerationExecutionSpecifications _specifications;
    MaterializationExecutionSpecification _tampered;
    Exception _error;

    void Establish()
    {
        _specifications = new(Globals.JsonSerializerOptions);
        var captured = _specifications.Capture(GenerationExecutions.Definition(), GenerationExecutions.ReadModel(), GenerationExecutions.EventSchemas(), new("sink-v1"));
        var content = JsonNode.Parse(captured.CanonicalContent)!;
        content["projection"]!["Unrecognized"] = "dropped on restore";
        _tampered = MaterializationExecutionSpecification.Capture(captured.EventSequence, GenerationExecutionSpecifications.Canonicalize(content.ToJsonString()), captured.RegisteringVersion);
    }

    void Because() => _error = Catch.Exception(() => _specifications.Restore(_tampered));

    [Fact] void should_have_a_valid_digest() => Catch.Exception(_tampered.EnsureValid).ShouldBeNull();
    [Fact] void should_refuse_the_lossy_restore() => _error.ShouldBeOfExactType<InvalidMaterializationExecution>();
}
