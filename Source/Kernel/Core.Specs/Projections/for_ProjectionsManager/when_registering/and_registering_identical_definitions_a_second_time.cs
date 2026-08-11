// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// The registration-storm shape: the same registration arrives again - a retry, another replica of the same client
/// version, or a request that was still queued when its sender gave up. The first request does the work; because the
/// grain is non-reentrant every duplicate runs after it, compares equal against the registered state and returns
/// without repeating the per-namespace fan-out.
/// </summary>
public class and_registering_identical_definitions_a_second_time : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;
    ProjectionDefinition _identicalDefinition;

    void Establish()
    {
        _definition = CreateDefinition("the-projection", "the-read-model");
        _identicalDefinition = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because()
    {
        await _grain.Register([_definition]);
        await _grain.Register([_identicalDefinition]);
    }

    [Fact] void should_only_register_with_the_engine_once() => _projectionsServiceClient.ReceivedWithAnyArgs(1).Register(default!, default!);
    [Fact] void should_only_set_the_projection_definition_once() => _projectionGrain.ReceivedWithAnyArgs(1).SetDefinition(default!);
    [Fact] void should_keep_the_first_registration_as_the_registered_state() => _state.Projections.ShouldContainOnly(_definition);
}
