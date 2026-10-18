// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_legacy_statistics_storage_is_refreshed : given.a_projections_manager_grain
{
    ProjectionDefinition[] _definitions;

    void Establish()
    {
        var lowered = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection));
        _definitions = lowered.Projections.ToArray();
        _readModelDefinitions = [lowered.ReadModel];
        _state.Projections = _definitions.Select(definition => definition with
        {
            HasLegacyStorageFields = true,
            Scope = ProjectionScope.Namespaced,
            FromEvery = definition.FromEvery with { Key = WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit }
        }).ToList();
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(call => comparer.Compare(call.Arg<ProjectionKey>(), call.ArgAt<ProjectionDefinition>(1), call.ArgAt<ProjectionDefinition>(2)));
    }

    async Task Because()
    {
        await _grain.Register(_definitions);
        await _grain.Register(_definitions);
    }

    [Fact] void should_keep_both_statistics_definitions() => _state.Projections.Count().ShouldEqual(2);
    [Fact] void should_persist_the_lossless_definitions() => _state.Projections.ShouldEachConformTo(definition => !definition.HasLegacyStorageFields);
    [Fact] void should_restore_the_global_definition() => _state.Projections.Select(definition => definition.Scope).ShouldContain(ProjectionScope.Global);
    [Fact] void should_register_with_the_engine_only_once() => _projectionsServiceClient.ReceivedWithAnyArgs(1).Register(default!, default!);
    [Fact] void should_refresh_each_projection_grain_once() => _projectionGrain.ReceivedWithAnyArgs(2).SetDefinition(default!);
    [Fact] void should_persist_the_manager_upgrade_once() => _stateStorage.Received(1).WriteStateAsync();
}
