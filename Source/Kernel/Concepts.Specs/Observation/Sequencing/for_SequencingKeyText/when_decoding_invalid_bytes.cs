// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyText;

public class when_decoding_invalid_bytes : Specification
{
    [Theory]
    [InlineData("ff")]
    [InlineData("c0af")]
    [InlineData("eda080")]
    [InlineData("f4908080")]
    [InlineData("f09f98")]
    public void should_reject_non_utf8_encodings(string hex) =>
        Catch.Exception(() => SequencingKeyText.Decode(Convert.FromHexString(hex))).ShouldBeOfExactType<InvalidCanonicalSequencingKey>();
}
