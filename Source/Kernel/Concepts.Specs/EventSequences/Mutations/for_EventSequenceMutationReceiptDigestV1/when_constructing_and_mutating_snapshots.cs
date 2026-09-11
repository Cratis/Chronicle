// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationReceiptDigestV1;

public class when_constructing_and_mutating_snapshots : Specification
{
    byte[] _source;
    EventSequenceMutationReceiptDigestV1 _digest;
    EventSequenceMutationReceiptDigestV1 _equalDigest;
    byte[] _firstSnapshot;
    byte[] _secondSnapshot;

    void Establish()
    {
        _source = Enumerable.Range(0, 32).Select(_ => (byte)_).ToArray();
        _equalDigest = new(_source);
        _digest = new(_source);
    }

    void Because()
    {
        _source[0] = 255;
        _firstSnapshot = _digest.Snapshot();
        _firstSnapshot[1] = 255;
        _secondSnapshot = _digest.Snapshot();
    }

    [Fact] void should_contain_exactly_32_bytes() => _secondSnapshot.Length.ShouldEqual(32);
    [Fact] void should_defensively_copy_the_source() => _secondSnapshot[0].ShouldEqual((byte)0);
    [Fact] void should_return_defensive_snapshots() => _secondSnapshot[1].ShouldEqual((byte)1);
    [Fact] void should_compare_by_content() => _digest.ShouldEqual(_equalDigest);
    [Fact] void should_hash_equal_content_equally() => _digest.GetHashCode().ShouldEqual(_equalDigest.GetHashCode());
}
