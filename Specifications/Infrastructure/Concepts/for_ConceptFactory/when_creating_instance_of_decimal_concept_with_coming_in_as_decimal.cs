// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_decimal_concept_with_coming_in_as_decimal : Specification
{
    DecimalConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DecimalConcept), 5d) as DecimalConcept;

    [Fact] void should_hold_the_decimal() => result.Value.ShouldEqual(5m);
}
