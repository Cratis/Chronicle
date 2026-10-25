// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationExecutionSpecifications;

public class when_restoring_captured_execution : Specification
{
    GenerationExecutionSpecifications _specifications;
    MaterializationExecutionSpecification _execution;
    MaterializationExecutionInputs _inputs;

    void Establish()
    {
        _specifications = new(Globals.JsonSerializerOptions);
        _execution = _specifications.Capture(GenerationExecutions.Definition(), GenerationExecutions.ReadModel(), GenerationExecutions.EventSchemas(), new("sink-v1"));
    }

    void Because() => _inputs = _specifications.Restore(_execution);

    [Fact] void should_restore_the_projection_identifier() => _inputs.Projection.Identifier.Value.ShouldEqual(GenerationExecutions.Projection);
    [Fact] void should_restore_the_projection_mappings() => _inputs.Projection.FromEvery.Properties["name"].ShouldEqual("snapshotName");
    [Fact] void should_restore_the_registering_version() => _inputs.Projection.Version.ShouldEqual("1.2.3");
    [Fact] void should_restore_the_read_model_container() => _inputs.ReadModel.ContainerName.Value.ShouldEqual("models");
    [Fact] void should_restore_the_event_schema_type() => _inputs.EventSchemas.Single().Type.ShouldEqual(new EventType("event", 1));
    [Fact] void should_restore_the_event_schema_origin() => _inputs.EventSchemas.Single().Origin.ShouldEqual("origin");
    [Fact] void should_restore_the_semantic_sink_revision() => _inputs.SemanticSinkRevision.ShouldEqual(new MaterializationRevision("sink-v1"));
    [Fact] void should_recapture_byte_equal() => _specifications.Capture(_inputs.Projection, _inputs.ReadModel, _inputs.EventSchemas, _inputs.SemanticSinkRevision).CanonicalContent.ShouldEqual(_execution.CanonicalContent);
}
