// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_kernel_projections_are_ensured_again : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;

    async Task Establish()
    {
        _definition = CreateDefinition("kernel-projection", "kernel-read-model") with { Owner = ProjectionOwner.Kernel };
        _readModelDefinitions = [CreateReadModelDefinition("kernel-read-model")];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        await _grain.Register([_definition], ProjectionOwner.Kernel);
        _projectionsServiceClient.ClearReceivedCalls();
        _projectionGrain.ClearReceivedCalls();
    }

    Task Because() => _grain.Register([_definition with { }], ProjectionOwner.Kernel);

    [Fact] void should_not_register_with_the_engine_again() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_not_set_the_projection_definition_again() => _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] void should_keep_one_kernel_projection() => _state.Projections.ShouldContainOnly(_definition);
}
