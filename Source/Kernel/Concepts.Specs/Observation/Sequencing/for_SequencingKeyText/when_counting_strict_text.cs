// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyText;

public class when_counting_strict_text : Specification
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("\u00e9", 2)]
    [InlineData("e\u0301", 3)]
    [InlineData("\U0001F600", 4)]
    [InlineData("\u07ff\u0800", 5)]
    public void should_count_without_normalizing(string text, int expected) => SequencingKeyText.ByteCount(text).ShouldEqual(expected);
    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("\U0001F600", 4)]
    public void should_accept_well_formed_text(string text, int _) => SequencingKeyText.IsWellFormed(text).ShouldBeTrue();
}
