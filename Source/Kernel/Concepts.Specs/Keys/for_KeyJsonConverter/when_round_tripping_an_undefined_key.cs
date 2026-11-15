// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Keys.for_KeyJsonConverter;

public class when_round_tripping_an_undefined_key : Specification
{
    JsonValueKind _serializedValueKind;
    Key _result;

    void Because()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new KeyJsonConverter());
        var json = JsonSerializer.Serialize(Key.Undefined, options);
        using var document = JsonDocument.Parse(json);
        _serializedValueKind = document.RootElement.GetProperty("value").ValueKind;
        _result = JsonSerializer.Deserialize<Key>(json, options)!;
    }

    [Fact] void should_write_a_null_value() => _serializedValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_restore_the_undefined_key() => ReferenceEquals(_result, Key.Undefined).ShouldBeTrue();
}
