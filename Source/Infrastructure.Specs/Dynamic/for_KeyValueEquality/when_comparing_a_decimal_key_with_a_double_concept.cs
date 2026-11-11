// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_KeyValueEquality;

public class when_comparing_a_decimal_key_with_a_double_concept : Specification
{
    record DoubleConcept(double Value) : ConceptAs<double>(Value);

    bool _result;

    void Because() => _result = KeyValueEquality.AreEqual(0.23m, new DoubleConcept(0.23d));

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
}
