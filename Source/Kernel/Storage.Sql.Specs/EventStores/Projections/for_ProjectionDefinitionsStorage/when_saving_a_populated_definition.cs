// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionsStorage;

public class when_saving_a_populated_definition : given.a_populated_definition
{
    ProjectionDefinition _restored;
    string _stored;
    string _restoredStored;

    async Task Because()
    {
        await _storage.Save(_definition);
        _restored = await _storage.Get(_definition.Identifier);
        _stored = _definition.ToSql().Definitions[1];
        _restoredStored = _restored.ToSql().Definitions[1];
    }

    [Fact] void should_store_the_documented_json() => JsonNode.DeepEquals(JsonNode.Parse(_stored), JsonNode.Parse(StoredJson.Populated)).ShouldBeTrue();
    [Fact] void should_restore_what_it_stored() => JsonNode.DeepEquals(JsonNode.Parse(_restoredStored), JsonNode.Parse(_stored)).ShouldBeTrue();
    [Fact] void should_restore_the_initial_model_state() => _restored.InitialModelState.ToJsonString().ShouldEqual(_definition.InitialModelState.ToJsonString());
    [Fact] void should_restore_the_event_types_of_the_projection() => _restored.From.Keys.ShouldContainOnly(_definition.From.Keys.ToArray());
    [Fact] void should_restore_the_children() => _restored.Children.Keys.ShouldContainOnly(_definition.Children.Keys.ToArray());
    [Fact] void should_restore_the_tags() => _restored.Tags.ShouldContainOnly("one", "two");
}
