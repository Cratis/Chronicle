// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_the_execution_snapshot_is_missing : given.a_generation_projection_resolver
{
    Exception _error;

    void Establish()
    {
        Bind(_binding);
        _builds.GetExecution(Store, _binding.Key, _binding.Identity.Generation).Returns((MaterializationExecutionSpecification?)null);
    }

    async Task Because() => _error = await Catch.Exception(async () => await Resolve(_binding.Identity.ObserverId));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidMaterializationExecution>();
    [Fact] void should_not_build_a_projection() => _projectionFactory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
