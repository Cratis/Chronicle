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
using MongoDB.Bson;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;
using static Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters.given.legacy_definition_documents;

using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_comparing_legacy_statistics_definitions : Specification
{
    readonly List<ProjectionDefinitionCompareResult> _direct = [];
    readonly List<ProjectionDefinitionCompareResult> _cached = [];
    readonly List<ProjectionDefinitionCompareResult> _listed = [];
    readonly List<ProjectionDefinitionCompareResult> _resaved = [];
    readonly List<ProjectionScope> _scopes = [];
    ProjectionDefinitionCompareResult _changedScope;

    async Task Because()
    {
        var storage = Substitute.For<IStorage>();
        var inner = Substitute.For<IProjectionDefinitionsStorage>();
        var cache = new CachingProjectionDefinitionsStorage(inner);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(cache);
        inner.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        var definitions = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections;
        var persisted = definitions.ToDictionary(definition => definition.Identifier, For);
        inner.Get(Arg.Any<ProjectionId>()).Returns(call => Restore(persisted[call.Arg<ProjectionId>()]));
        inner.GetAll().Returns(_ => persisted.Values.Select(Restore).ToArray());
        inner.Save(Arg.Any<KernelDefinition>()).Returns(call =>
        {
            var definition = call.Arg<KernelDefinition>();
            persisted[definition.Identifier] = definition.ToMongoDB().ToBsonDocument();
            return Task.CompletedTask;
        });
        foreach (var definition in definitions)
        {
            var key = new ProjectionKey(definition.Identifier, "store");
            _scopes.Add(definition.Scope);
            _direct.Add(await comparer.Compare(key, Restore(persisted[definition.Identifier]), definition));
            _cached.Add(await comparer.Compare(key, await cache.Get(definition.Identifier), definition));
        }
        foreach (var legacy in await cache.GetAll())
        {
            var definition = definitions.Single(definition => definition.Identifier == legacy.Identifier);
            var key = new ProjectionKey(definition.Identifier, "store");
            _listed.Add(await comparer.Compare(key, await cache.Get(definition.Identifier), definition));
            await cache.Save(definition);
            _resaved.Add(await comparer.Compare(key, Restore(persisted[definition.Identifier]), definition));
        }
        var global = definitions.Single(definition => definition.Scope == ProjectionScope.Global);
        _changedScope = await comparer.Compare(new(global.Identifier, "store"), Restore(persisted[global.Identifier]), global with { Scope = ProjectionScope.Namespaced });
    }

    [Fact] void should_cover_both_statistics_projections() => _scopes.Count.ShouldEqual(2);
    [Fact] void should_cover_namespaced_statistics() => _scopes.ShouldContain(ProjectionScope.Namespaced);
    [Fact] void should_cover_global_statistics() => _scopes.ShouldContain(ProjectionScope.Global);
    [Fact] void should_not_evolve_legacy_statistics_on_first_registration() => _direct.ShouldEachConformTo(result => result == ProjectionDefinitionCompareResult.Same);
    [Fact] void should_not_lose_legacy_knowledge_on_a_cached_read() => _cached.ShouldEachConformTo(result => result == ProjectionDefinitionCompareResult.Same);
    [Fact] void should_not_lose_legacy_knowledge_when_listing_refreshes_the_cache() => _listed.ShouldEachConformTo(result => result == ProjectionDefinitionCompareResult.Same);
    [Fact] void should_round_trip_the_freshly_saved_definitions() => _resaved.ShouldEachConformTo(result => result == ProjectionDefinitionCompareResult.Same);
    [Fact] void should_detect_a_real_scope_change_after_the_lossless_save() => _changedScope.ShouldEqual(ProjectionDefinitionCompareResult.Different);
}
