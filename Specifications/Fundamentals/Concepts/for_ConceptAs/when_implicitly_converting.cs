// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts.given;

namespace Aksio.Cratis.Concepts.for_ConceptAs;

public class when_implicitly_converting
{
    [Fact]
    public void should_convert_from_primitive_to_concept()
    {
        var intValue = 42;
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