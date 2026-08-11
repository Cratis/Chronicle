// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_all_definitions_are_unchanged : given.a_projections_manager_grain
{
    ProjectionDefinition _existing;
    ProjectionDefinition _incoming;

    void Establish()
    {
        _existing = CreateDefinition("the-projection", "the-read-model");
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _state.Projections = [_existing];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact] void should_not_register_with_the_engine() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_not_set_any_projection_definition() => _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] void should_not_touch_any_observer() => _observerGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_leave_the_registered_definitions_untouched() => _state.Projections.ShouldContainOnly(_existing);
}
