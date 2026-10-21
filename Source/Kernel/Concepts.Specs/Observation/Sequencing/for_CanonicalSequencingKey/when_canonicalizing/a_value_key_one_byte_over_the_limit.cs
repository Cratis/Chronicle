// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class a_value_key_one_byte_over_the_limit : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.From(new ValueSequencingKey([new string('a', 8188)])));

    [Fact] void should_reject_the_value_key() => _error.ShouldBeOfExactType<SequencingKeyTooLarge>();
}
