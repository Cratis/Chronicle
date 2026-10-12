// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class a_reversed_surrogate_pair : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => CanonicalSequencingKey.From(new ValueSequencingKey(["\uDC00\uD800"])));

    [Fact] void should_reject_the_value_key() => _error.ShouldBeOfExactType<InvalidSequencingKeyText>();
}
