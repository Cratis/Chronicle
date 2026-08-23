// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_projection_setup_succeeds_on_retry : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;
    Exception _firstException;
    Exception _secondException;

    void Establish()
    {
        _definition = CreateDefinition("projection", "read-model");
        _readModelDefinitions = [CreateReadModelDefinition("read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.New);
        _projectionGrain
            .SetDefinition(_definition)
            .Returns(
                Task.FromException(new InvalidOperationException("Projection grain unavailable")),
                Task.CompletedTask);
    }

    async Task Because()
    {
        _firstException = await Catch.Exception(() => _grain.Register([_definition]));
        _secondException = await Catch.Exception(() => _grain.Register([_definition]));
    }

    [Fact] void should_report_the_first_failure() => _firstException.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_not_fail_the_retry() => _secondException.ShouldBeNull();
    [Fact] void should_retry_the_engine_registration() => _projectionsServiceClient.Received(2).Register((Concepts.EventStoreName)EventStore, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _definition })));
    [Fact] void should_retry_setting_the_projection_definition() => _projectionGrain.Received(2).SetDefinition(_definition);
    [Fact] void should_register_the_definition_after_the_retry() => _state.Projections.ShouldContainOnly(_definition);
}
