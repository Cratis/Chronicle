// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventMetadataConverters;

public class when_round_tripping_event_metadata : Specification
{
    EventMetadata _original;
    EventMetadata _result;

    void Establish() =>
        _original = new(42, new EventType("type", 1));

    void Because() => _result = _original.ToContract().ToClient();

    [Fact] void should_preserve_sequence_number() => _result.SequenceNumber.ShouldEqual(_original.SequenceNumber);

    [Fact] void should_preserve_type_id() => _result.Type.Id.ShouldEqual(_original.Type.Id);

    [Fact] void should_preserve_type_generation() => _result.Type.Generation.ShouldEqual(_original.Type.Generation);
}
