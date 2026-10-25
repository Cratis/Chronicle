// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_reading_a_generation_execution_snapshot : Specification
{
    ReadModelDefinition _resolved;
    ReadModelDefinition _afterCallerMutation;

    void Because()
    {
        var context = GenerationContexts.Create(WellKnownSinkTypes.InMemory, generation: 2, schemaGeneration: 7);
        _resolved = context.GetReadModel("store", "namespace");
        _resolved.Schemas.Clear();
        _afterCallerMutation = context.GetReadModel("store", "namespace");
    }

    [Fact] void should_detach_the_schema_from_caller_mutation() => _afterCallerMutation.LatestGeneration.ShouldEqual(new ReadModelGeneration(7));
    [Fact] void should_keep_the_captured_index_set() => _afterCallerMutation.Indexes.Single().Properties.Single().Path.Path.ShouldEqual("name");
    [Fact] void should_address_the_materialization_destination_not_the_schema_generation() => _afterCallerMutation.ContainerName.ShouldEqual(Concepts.Projections.Generations.MaterializationGenerationIdentity.For("projection", 2, "models").Destination);
}
