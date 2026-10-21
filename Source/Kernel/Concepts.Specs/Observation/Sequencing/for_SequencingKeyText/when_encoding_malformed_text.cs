// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyText;

public class when_encoding_malformed_text : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SequencingKeyText.ByteCount("\uD800x"));

    [Fact] void should_reject_the_text() => _error.ShouldBeOfExactType<InvalidSequencingKeyText>();
    [Fact] void should_not_consider_the_text_well_formed() => SequencingKeyText.IsWellFormed("\uD800x").ShouldBeFalse();
    [Fact] void should_export_the_raw_code_units() => Convert.ToHexString(SequencingKeyText.Utf16LittleEndian("\uD800x")).ShouldEqual("00D87800");
}
