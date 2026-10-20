// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_kernel_definitions : Specification
{
    readonly List<ProjectionDefinitionCompareResult> _results = [];
    readonly List<ProjectionScope> _scopes = [];

    async Task Because()
    {
        var storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IProjectionDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(definitions);
        definitions.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        var namespaced = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        var legacy = namespaced with { Identifier = $"{namespaced.Identifier.Value}.global", Scope = ProjectionScope.Global };
        var kernelDefinitions = KernelProjectionDeclarations.Discover(typeof(EventTypeStatisticsProjection).Assembly.GetTypes())
            .SelectMany(declaration => KernelProjectionDeclarations.Lower(declaration).Projections)
            .Append(legacy);
        foreach (var definition in kernelDefinitions)
        {
            var restored = definition.ToSql().ToKernel();
            _scopes.Add(restored.Scope);
            _results.Add(await comparer.Compare(new(definition.Identifier, "test-store"), definition, restored));
        }
    }

    [Fact] void should_exercise_at_least_one_definition() => _results.ShouldNotBeEmpty();
    [Fact] void should_preserve_the_global_scope() => _scopes.ShouldContain(ProjectionScope.Global);
    [Fact] void should_compare_every_definition_as_unchanged() => _results.ShouldEachConformTo(result => result == ProjectionDefinitionCompareResult.Same);
}
