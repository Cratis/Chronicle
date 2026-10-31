// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_comparing_legacy_definition_rules : given.a_populated_definition
{
    ProjectionDefinitionCompareResult _unchanged;
    ProjectionDefinitionCompareResult _changedFrom;
    bool _stillLegacy;
    bool _freshIsLossless;

    async Task Because()
    {
        var legacy = given.legacy_definition_documents.Restore(given.legacy_definition_documents.For(_definition));
        var storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IProjectionDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(definitions);
        definitions.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        var key = new ProjectionKey(_definition.Identifier, "store");
        _unchanged = await comparer.Compare(key, legacy, _definition);
        var changedFrom = _definition.From.ToDictionary(pair => pair.Key, pair => pair.Value with { Key = "ChangedKey" });
        _changedFrom = await comparer.Compare(key, legacy, _definition with { From = changedFrom });
        _stillLegacy = given.legacy_definition_documents.Restore(legacy.ToMongoDB().ToBsonDocument()).HasLegacyStorageFields;
        _freshIsLossless = !given.legacy_definition_documents.Restore(_definition.ToMongoDB().ToBsonDocument()).HasLegacyStorageFields;
    }

    [Fact] void should_ignore_only_the_unknown_legacy_rules_including_child_every_event_settings() => _unchanged.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_detect_a_real_from_mapping_change() => _changedFrom.ShouldEqual(ProjectionDefinitionCompareResult.Different);
    [Fact] void should_not_promote_unreconciled_defaults_to_known_rules_on_save() => _stillLegacy.ShouldBeTrue();
    [Fact] void should_mark_a_fresh_save_as_lossless() => _freshIsLossless.ShouldBeTrue();
}
