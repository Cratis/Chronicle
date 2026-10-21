// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_VersionedEnvelope.when_mutating_the_source_bytes_after_creating;

public class and_the_bytes_were_copied : Specification
{
    byte[] _source;
    VersionedEnvelope _envelope;

    void Establish()
    {
        _source = [1, 2, 3];
        _envelope = new(1, _source.AsSpan());
    }

    void Because() => _source[0] = 9;

    [Fact] void should_keep_the_original_bytes() => _envelope.Payload[0].ShouldEqual((byte)1);
    [Fact] void should_compare_by_content() => _envelope.ShouldEqual(new VersionedEnvelope(1, new byte[] { 1, 2, 3 }.AsSpan()));
    [Fact] void should_hash_by_content() => _envelope.GetHashCode().ShouldEqual(new VersionedEnvelope(1, new byte[] { 1, 2, 3 }.AsSpan()).GetHashCode());
}
