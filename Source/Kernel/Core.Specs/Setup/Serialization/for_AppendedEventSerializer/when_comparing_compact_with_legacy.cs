// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_comparing_compact_with_legacy : given.a_compact_serializer
{
    AppendedEvent[] _legacy;
    AppendedEvent[] _compact;
    WireType _emptyWireType;

    void Because()
    {
        configuration.Clustering.EventWireFormat = EventWireFormat.Json;
        _legacy = event_batch.Select(item => serializer.Deserialize(serializer.SerializeToArray(item))!).ToArray();
        configuration.Clustering.EventWireFormat = EventWireFormat.Compact;
        _compact = event_batch.Select(item => serializer.Deserialize(serializer.SerializeToArray(item))!).ToArray();
        _emptyWireType = WireTypeOf(serializer.SerializeToArray(event_batch[2]));
    }

    [Fact] void should_match_the_legacy_semantics_for_every_normalized_shape() => Enumerable.Range(0, event_batch.Length).ToList().ForEach(index => AssertEquivalent(_compact[index], _legacy[index]));
    [Fact] void should_exercise_the_compact_path() => _emptyWireType.ShouldEqual(WireType.TagDelimited);
}
