// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_skipping_unknown_compact_fields : given.a_compact_serializer
{
    AppendedEvent _result;

    void Because() => _result = serializer.Deserialize(CompactMessage(1, [1, 0, 0, 0, 0], extraField: true))!;

    [Fact] void should_read_the_envelope() => JsonSerializer.Serialize(_result.Context, options).ShouldEqual(JsonSerializer.Serialize(appended_event.Context, options));
    [Fact] void should_read_the_content() => ((IDictionary<string, object?>)_result.Content).Count.ShouldEqual(0);
}
