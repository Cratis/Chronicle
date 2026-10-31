// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_an_old_silo_still_includes_stored_global_statistics : given.a_projections_manager_grain
{
    ProjectionDefinition _remaining;
    ProjectionDefinition _legacy;

    void Establish()
    {
        _remaining = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        _legacy = _remaining with { Identifier = $"{_remaining.Identifier.Value}.global", Scope = ProjectionScope.Global };
        _state.Projections = [_remaining, _legacy];
        _namespaces.GetAll().Returns(["first", "second"]);
        _readModelDefinitions = [CreateReadModelDefinition(_remaining.ReadModel, nameof(EventTypeStatistics))];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_remaining, _legacy], ProjectionOwner.Kernel);

    [Fact] void should_remove_the_stored_global_definition() => _state.Projections.ShouldContainOnly(_remaining);
    [Fact] async Task should_retire_its_observers_in_every_namespace() => await _observerGrain.Received(2).Retire();
    [Fact] async Task should_unregister_the_shared_writer() => await _projectionsServiceClient.Received(1).Unregister((EventStoreName)EventStore, _legacy.Identifier);
    [Fact] async Task should_remove_the_legacy_definition_from_storage() => await _projectionGrain.Received(1).Remove();
    [Fact] void should_still_be_able_to_read_a_legacy_definition() => _legacy.Scope.ShouldEqual(ProjectionScope.Global);
}
