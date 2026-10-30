// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.for_ReadModelIndexesJson;

public class when_reading_ordered_indexes : Specification
{
    IndexDefinition[] _original;
    IndexDefinition[] _roundTripped;
    string _json;

    void Establish() => _original = [new("name"), new("created") { Properties = [new("created", true)] }, new("name") { Properties = [new("name"), new("details.total", true)] }];

    void Because()
    {
        _json = ReadModelIndexesJson.Serialize(_original);
        _roundTripped = ReadModelIndexesJson.Deserialize(_json);
    }

    [Fact] void should_round_trip_every_key_and_direction() => _roundTripped.ShouldEqual(_original);
    [Fact] void should_preserve_the_first_property_for_older_kernels() => JsonNode.Parse(_json).AsArray().Select(index => index["properties"][0]["path"].GetValue<string>()).SequenceEqual(["name", "created", "name"]).ShouldBeTrue();
    [Fact] void should_read_the_original_object_format_as_ascending() => ReadModelIndexesJson.Deserialize("""[{"properties":[{"path":"name"}]}]""").SequenceEqual([new IndexDefinition("name")]).ShouldBeTrue();
    [Fact] void should_skip_malformed_legacy_objects() => ReadModelIndexesJson.Deserialize("""[{"properties":[{}]},{"properties":[null]},{"properties":[{"path":12}]}]""").ShouldBeEmpty();
    [Fact] void should_not_partially_read_a_malformed_composite() => ReadModelIndexesJson.Deserialize("""[{"properties":[{"path":"name"},{}]}]""").ShouldBeEmpty();
    [Fact] void should_skip_invalid_directions() => ReadModelIndexesJson.Deserialize("""[{"properties":[{"path":"name","descending":"true"}]}]""").ShouldBeEmpty();
    [Fact] void should_read_a_mixture_of_old_strings_and_ordered_objects() => ReadModelIndexesJson.Deserialize("""["name",{"properties":[{"path":"created","descending":true}]}]""").ShouldEqual(_original.Take(2));
}
