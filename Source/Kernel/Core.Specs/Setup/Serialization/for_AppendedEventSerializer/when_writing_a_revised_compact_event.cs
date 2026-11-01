// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_writing_a_revised_compact_event : given.a_compact_serializer
{
    AppendedEvent _input;
    AppendedEvent _result;
    byte[] _bytes;

    void Establish() => _input = event_batch[2] with
    {
        Context = appended_event.Context with { Hash = new("unchanged-durable-hash") },
        OriginalContent = "{\"text\":\"original\"}",
        Revisions = [new(new(2), appended_event.Context.CorrelationId, appended_event.Context.Causation, appended_event.Context.CausedBy, appended_event.Context.Occurred, "{\"text\":\"revision\"}")],
        GenerationalContent = new Dictionary<int, string> { [1] = "{\"text\":\"generation-one\"}" },
        GenerationalHashes = new Dictionary<int, EventHash> { [1] = new("generation-one-hash") },
        RevisedGeneration = new(2)
    };

    void Because()
    {
        _bytes = serializer.SerializeToArray(_input);
        _result = serializer.Deserialize(_bytes)!;
    }

    [Fact] void should_use_compact() => WireTypeOf(_bytes).ShouldEqual(WireType.TagDelimited);
    [Fact] void should_preserve_the_complete_envelope_and_durable_hashes() => AssertEquivalent(_result, _input);
}
