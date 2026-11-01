// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_the_other_wire_format : given.a_compact_serializer
{
    AppendedEvent _legacy;
    AppendedEvent _compact;

    void Because()
    {
        _legacy = serializer.Deserialize(Convert.FromBase64String(given.LegacyWireFixtures.Single))!;
        var compact = serializer.SerializeToArray(event_batch[2]);
        configuration.Clustering.EventWireFormat = EventWireFormat.Json;
        _compact = serializer.Deserialize(compact)!;
    }

    [Fact] void should_read_legacy_bytes_while_writing_compact() => AssertEquivalent(_legacy, appended_event);
    [Fact] void should_read_compact_bytes_while_writing_json() => AssertEquivalent(_compact, event_batch[2]);
}
