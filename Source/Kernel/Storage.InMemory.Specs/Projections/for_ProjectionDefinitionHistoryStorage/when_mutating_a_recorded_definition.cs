// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_mutating_a_recorded_definition : given.a_mutable_history_entry
{
    async Task Because()
    {
        await _storage.Record(_entry);
        Mutate(_entry.Definition);
        await Read();
    }

    [Fact] void should_preserve_json_state() => _entries[0].Definition.InitialModelState["nested"]!["value"]!.GetValue<string>().ShouldEqual("original");
    [Fact] void should_preserve_event_generations() => _entries[0].Definition.From.Keys.ShouldContain(_second);
    [Fact] void should_preserve_from_properties() => _entries[0].Definition.From[_first].Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_join_properties() => _entries[0].Definition.Join[_first].Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_child_properties() => _entries[0].Definition.Children["children"].From[_first].Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_child_every_event_properties() => _entries[0].Definition.Children["children"].All.Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_nested_properties() => _entries[0].Definition.Nested!["nested"].FromEvery.Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_derivative_properties() => _entries[0].Definition.FromDerivatives.Single().From.Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_derivative_event_types() => _entries[0].Definition.FromDerivatives.Single().EventTypes.First().ShouldEqual(_first);
    [Fact] void should_preserve_every_event_properties() => _entries[0].Definition.FromEvery.Properties["name"].ShouldEqual("original");
    [Fact] void should_preserve_every_event_key() => _entries[0].Definition.FromEvery.Key.Value.ShouldEqual("$eventSourceId");
    [Fact] void should_preserve_tags() => _entries[0].Definition.Tags!.Single().ShouldEqual("original");
    [Fact] void should_preserve_excluded_paths() => _entries[0].Definition.NoAutoMapProperties!.Single().ShouldEqual((Cratis.Chronicle.Properties.PropertyPath)"original");
    [Fact] void should_preserve_identity_and_metadata() => (_entries[0] with { Definition = _entry.Definition }).ShouldEqual(_entry);
    [Fact] void should_preserve_projection_concept() => _entries[0].Definition.Identifier.ShouldEqual(_entry.Definition.Identifier);
}
