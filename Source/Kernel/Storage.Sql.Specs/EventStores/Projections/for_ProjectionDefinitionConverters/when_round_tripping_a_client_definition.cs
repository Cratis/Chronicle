// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_a_client_definition : Specification
{
    ProjectionDefinition _restored;
    ProjectionDefinitionCompareResult _result;

    async Task Because()
    {
        var template = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single(definition => definition.Scope == ProjectionScope.Global);
        var definition = template with
        {
            Owner = ProjectionOwner.Client,
            Identifier = "client-projection",
            FromEvery = template.FromEvery with { Key = "Category", AutoMap = AutoMap.Disabled },
            NoAutoMapProperties = null
        };
        _restored = definition.ToSql().ToKernel();
        var storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IProjectionDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(definitions);
        definitions.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _result = await comparer.Compare(new(definition.Identifier, "store"), definition, _restored);
    }

    [Fact] void should_preserve_global_scope() => _restored.Scope.ShouldEqual(ProjectionScope.Global);
    [Fact] void should_preserve_the_declared_key() => _restored.FromEvery.Key.Value.ShouldEqual("Category");
    [Fact] void should_preserve_the_every_event_auto_map_setting() => _restored.FromEvery.AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_preserve_null_auto_map_exclusions() => _restored.NoAutoMapProperties.ShouldBeNull();
    [Fact] void should_compare_as_unchanged() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
