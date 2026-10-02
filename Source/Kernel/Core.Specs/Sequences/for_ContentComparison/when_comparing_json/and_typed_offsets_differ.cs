// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_ContentComparison.when_comparing_json;

public class and_typed_offsets_differ : Specification
{
    bool _result;

    void Because()
    {
        var value = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromMinutes(150));
        _result = ContentComparison.Equals(JsonValue.Create(value), JsonValue.Create(value.ToUniversalTime()));
    }

    [Fact] void should_compare_serialized_strings_not_clr_instants() => _result.ShouldBeFalse();
}
