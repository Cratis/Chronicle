// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_resolving_a_bound_generation : given.a_generation_projection_resolver
{
    GenerationExecution? _result;

    void Establish() => Bind(_binding);

    async Task Because() => _result = await Resolve(_binding.Identity.ObserverId);

    [Fact] void should_resolve_the_binding() => _result!.Binding.ShouldEqual(_binding);
    [Fact] void should_carry_the_persisted_execution() => _result!.Execution.ShouldEqual(_execution);
    [Fact] void should_carry_the_built_projection() => _result!.Projection.ShouldEqual(_projection);
    [Fact] void should_build_from_the_snapshot_definition() => _projectionFactory.Received(1).Create(
        Store,
        GenerationExecutions.Namespace,
        Arg.Is<ProjectionDefinition>(_ => _.Identifier == GenerationExecutions.Projection && _.FromEvery.Properties["name"] == "snapshotName"),
        Arg.Is<ReadModelDefinition>(_ => _.ContainerName == "models"),
        Arg.Is<IEnumerable<EventTypeSchema>>(_ => _.Single().Type == new EventType("event", 1)));
}
