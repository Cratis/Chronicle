// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_a_batch_mixes_compact_and_legacy : given.a_compact_serializer
{
    List<AppendedEvent> _result;
    WireType _unsupportedWireType;
    WireType _supportedWireType;

    void Because()
    {
        _unsupportedWireType = WireTypeOf(serializer.SerializeToArray(event_batch[0]));
        _supportedWireType = WireTypeOf(serializer.SerializeToArray(event_batch[2]));
        var batchSerializer = service_provider.GetRequiredService<Serializer<List<AppendedEvent>>>();
        _result = batchSerializer.Deserialize(batchSerializer.SerializeToArray(event_batch.ToList()))!;
    }

    [Fact] void should_fall_back_for_geometry() => _unsupportedWireType.ShouldEqual(WireType.LengthPrefixed);
    [Fact] void should_keep_supported_events_compact() => _supportedWireType.ShouldEqual(WireType.TagDelimited);
    [Fact] void should_preserve_all_events_in_order() => Enumerable.Range(0, event_batch.Length).ToList().ForEach(index => AssertEquivalent(_result[index], event_batch[index]));
}
