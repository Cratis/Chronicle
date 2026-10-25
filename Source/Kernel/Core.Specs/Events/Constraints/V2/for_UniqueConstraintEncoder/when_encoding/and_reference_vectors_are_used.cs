// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_reference_vectors_are_used : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    readonly UniqueConstraintComponent[][] _inputs =
    [
        [new("x")],
        [new(Value: null), new("")],
        [new(new string?[] { "A", null, "é" })]
    ];
    string[] _preimages;
    string[] _values;

    void Because()
    {
        _preimages = _inputs.Select(_ => Convert.ToHexString(UniqueConstraintEncoder.GetPreimage(_)).ToLowerInvariant()).ToArray();
        _values = _inputs.Select(_ => UniqueConstraintEncoder.Encode(_key, _).AsT0.Value).ToArray();

        // Preimages were written independently from the framing grammar; hashes computed with Node crypto.
    }

    [Fact] void should_frame_a_single_scalar() => _preimages[0].ShouldEqual("6368726f6e69636c652f756e697175652f76320000000001010000000178");
    [Fact] void should_hash_a_single_scalar() => _values[0].ShouldEqual("1db63c4279b32526344aecc2769e7d630f974e5f6d7d68cace455a4c66f8359c");
    [Fact] void should_frame_null_and_empty_separately() => _preimages[1].ShouldEqual("6368726f6e69636c652f756e697175652f76320000000002000100000000");
    [Fact] void should_hash_null_and_empty() => _values[1].ShouldEqual("0a60ea56b3d2db3a37ded963e11234a67ce81fa34570b34f6e221f3490dad9ec");
    [Fact] void should_count_array_items_and_utf8_bytes() => _preimages[2].ShouldEqual("6368726f6e69636c652f756e697175652f763200000000010200000003010000000141000100000002c3a9");
    [Fact] void should_hash_an_array() => _values[2].ShouldEqual("771b19df622907606cef0fa5acafbdf82329b004284d3d3af0bae05972f4c58d");
}
