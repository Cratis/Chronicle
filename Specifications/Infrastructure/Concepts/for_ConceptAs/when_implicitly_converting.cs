// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts.given;

namespace Cratis.Concepts.for_ConceptAs;

public class when_implicitly_converting
{
    [Fact]
    public void should_convert_from_primitive_to_concept()
    {
        const int intValue = 42;
        IntConcept conceptValue = intValue;

        intValue.ShouldEqual(conceptValue.Value);
    }

    [Fact]
    public void ShouldConvertFromConceptToPrimitive()
    {
        IntConcept conceptValue = new(42);
        int intValue = conceptValue;

        conceptValue.Value.ShouldEqual(intValue);
    }
}
