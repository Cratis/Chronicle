// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeySliceHash;

public class when_hashing_reference_inputs : Specification
{
    [Theory]
    [InlineData("", 0x02CC5D05U)]
    [InlineData("abc", 0x32D153FFU)]
    [InlineData("Nobody inspects the spammish repetition", 0xE2293B2FU)]
    public void should_match_the_published_vectors(string text, uint expected) =>
        SequencingKeySliceHash.Hash(Encoding.UTF8.GetBytes(text)).ShouldEqual(expected);

    [Theory]
    [InlineData(0, 0x02CC5D05U)]
    [InlineData(1, 0x002E0D32U)]
    [InlineData(3, 0xACA17380U)]
    [InlineData(4, 0x073FAA82U)]
    [InlineData(15, 0x9F29F87BU)]
    [InlineData(16, 0x3F6C9665U)]
    [InlineData(17, 0xE048ECDBU)]
    [InlineData(31, 0x00F1525CU)]
    [InlineData(32, 0xEE42E668U)]
    [InlineData(33, 0x770B5126U)]
    [InlineData(8192, 0x995AA16CU)]
    public void should_match_the_reference_at_lane_boundaries(int length, uint expected)
    {
        var bytes = new byte[length];
        for (var index = 0; index < length; index++)
        {
            bytes[index] = (byte)(((index * 31) + 7) & 0xFF);
        }

        SequencingKeySliceHash.Hash(bytes).ShouldEqual(expected);
    }
}
