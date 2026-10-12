// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_VersionedEnvelope.when_mutating_the_source_bytes_after_creating;

public class and_the_immutable_input_aliases_an_array : Specification
{
    byte[] _source;
    VersionedEnvelope _envelope;

    void Establish()
    {
        _source = [1, 2, 3];
        _envelope = new(1, ImmutableCollectionsMarshal.AsImmutableArray(_source));
    }

    void Because() => _source[0] = 9;

    [Fact] void should_copy_the_immutable_input_at_construction() => _envelope.Payload[0].ShouldEqual((byte)1);
}
