// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

using StoredChildren = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ChildrenDefinition;
using StoredDefinition = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinition;
using StoredEvery = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.FromEveryDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_writing_for_the_previous_kernel : Specification
{
    readonly List<BsonDocument> _documents = [];

    void Because()
    {
        var namespaced = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        var legacy = namespaced with { Identifier = $"{namespaced.Identifier.Value}.global", Scope = ProjectionScope.Global };
        foreach (var definition in new[] { namespaced, legacy })
        {
            _documents.Add(definition.ToMongoDB().ToBsonDocument());
        }
    }

    [Fact] void should_never_write_null_nested_collections() => _documents.ShouldEachConformTo(document => document["nested"].IsBsonDocument);
    [Fact] void should_never_write_null_exclusions() => _documents.ShouldEachConformTo(document => document["noAutoMapProperties"].IsBsonArray);
    [Fact] void should_never_write_null_tags() => _documents.ShouldEachConformTo(document => document["tags"].IsBsonArray);
    [Fact] void should_ignore_additive_root_storage_fields_in_the_unchanged_class_map_conventions() => BsonClassMap.LookupClassMap(typeof(StoredDefinition)).IgnoreExtraElements.ShouldBeTrue();
    [Fact] void should_ignore_additive_child_storage_fields_in_the_unchanged_class_map_conventions() => BsonClassMap.LookupClassMap(typeof(StoredChildren)).IgnoreExtraElements.ShouldBeTrue();
    [Fact] void should_ignore_additive_every_event_storage_fields_in_the_unchanged_class_map_conventions() => BsonClassMap.LookupClassMap(typeof(StoredEvery)).IgnoreExtraElements.ShouldBeTrue();
}
