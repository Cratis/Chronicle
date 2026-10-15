// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_parsing_stored_bytes;

public class with_an_invalid_utf8_byte : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.FromStored(Convert.FromHexString("0000000001ff")));

    [Fact] void should_reject_noncanonical_bytes() => _error.ShouldBeOfExactType<InvalidCanonicalSequencingKey>();
}
