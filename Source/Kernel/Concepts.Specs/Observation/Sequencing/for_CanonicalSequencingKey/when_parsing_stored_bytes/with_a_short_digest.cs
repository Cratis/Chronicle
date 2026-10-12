// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_parsing_stored_bytes;

public class with_a_short_digest : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.FromStored(Convert.FromHexString("0101" + new string('0', 62))));

    [Fact] void should_reject_noncanonical_bytes() => _error.ShouldBeOfExactType<InvalidCanonicalSequencingKey>();
}
