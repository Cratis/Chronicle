// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys.for_KeyJsonConverter;

public class when_round_tripping_a_key_with_array_indexers : Specification
{
    Key _key;
    Key _result;
    JsonElement _serializedIndexers;

    void Establish() => _key = new Key("parent", new ArrayIndexers([
        new ArrayIndexer("items", "id", "first"),
        new ArrayIndexer("items.children", "childId", "second")
    ]));

    void Because()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new KeyJsonConverter());
        var json = JsonSerializer.Serialize(_key, options);
        using var document = JsonDocument.Parse(json);
        _serializedIndexers = document.RootElement.GetProperty("arrayIndexers").Clone();
        _result = JsonSerializer.Deserialize<Key>(json, options)!;
    }

    [Fact] void should_restore_the_key() => _result.ShouldEqual(_key);
    [Fact] void should_restore_both_array_indexers() => _result.ArrayIndexers.All.ShouldEqual(_key.ArrayIndexers.All);
    [Fact] void should_write_the_first_indexers_own_value() => _serializedIndexers[0].GetProperty("value").GetString().ShouldEqual("first");
    [Fact] void should_write_the_second_indexers_own_value() => _serializedIndexers[1].GetProperty("value").GetString().ShouldEqual("second");
}
