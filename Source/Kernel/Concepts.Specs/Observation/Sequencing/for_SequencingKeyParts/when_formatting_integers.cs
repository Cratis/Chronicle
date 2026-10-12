// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyParts;

public class when_formatting_integers : Specification
{
    [Theory]
    [InlineData(long.MinValue, "-9223372036854775808")]
    [InlineData(long.MaxValue, "9223372036854775807")]
    [InlineData(0L, "0")]
    public void should_format_signed_64_bit_values(long value, string expected) => SequencingKeyParts.Canonical(value).ShouldEqual(expected);
    [Theory]
    [InlineData(ulong.MaxValue, "18446744073709551615")]
    [InlineData(0UL, "0")]
    public void should_format_unsigned_64_bit_values(ulong value, string expected) => SequencingKeyParts.Canonical(value).ShouldEqual(expected);
    [Theory]
    [InlineData(int.MinValue, "-2147483648")]
    [InlineData(int.MaxValue, "2147483647")]
    [InlineData(0, "0")]
    public void should_format_signed_32_bit_values(int value, string expected) => SequencingKeyParts.Canonical(value).ShouldEqual(expected);
}
