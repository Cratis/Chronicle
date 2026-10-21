// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_an_old_silo_registers_global_statistics_again : given.a_projections_manager_grain
{
    ProjectionDefinition _remaining;
    ProjectionDefinition _legacy;
    readonly List<KeyValuePair<string, object?>[]> _warnings = [];

    void Establish()
    {
        _remaining = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        _legacy = _remaining with { Identifier = $"{_remaining.Identifier.Value}.global", Scope = ProjectionScope.Global };
        _state.Projections = [_remaining, _legacy];
        _namespaces.GetAll().Returns(["first", "second"]);
        _readModelDefinitions = [CreateReadModelDefinition(_remaining.ReadModel, nameof(EventTypeStatistics))];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object)!, default, default!))
            .Do(call =>
            {
                if (call.ArgAt<LogLevel>(0) == LogLevel.Warning)
                {
                    // The generated logger clears its pooled state after Log returns.
                    _warnings.Add(call.ArgAt<IEnumerable<KeyValuePair<string, object?>>>(2).ToArray());
                }
            });
    }

    async Task Because()
    {
        await _grain.Register([_remaining], ProjectionOwner.Kernel);
        await _grain.Register([_remaining, _legacy], ProjectionOwner.Kernel);
    }

    [Fact] void should_refuse_the_unversioned_old_global_registration() => _state.Projections.ShouldContainOnly(_remaining);
    [Fact] async Task should_not_register_the_shared_writer() => await _projectionsServiceClient.DidNotReceive().Register(
        Arg.Any<EventStoreName>(), Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Any(definition => definition.Identifier == _legacy.Identifier)));
    [Fact] void should_warn_with_the_projection_identifier() => _warnings.Exists(fields =>
        fields.Any(field => field.Key == "Identifier" && Equals(field.Value, _legacy.Identifier))).ShouldBeTrue();
}
