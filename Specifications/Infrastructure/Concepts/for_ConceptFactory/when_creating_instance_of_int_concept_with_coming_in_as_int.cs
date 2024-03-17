// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_int_concept_with_coming_in_as_int : Specification
{
    IntConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(IntConcept), 5) as IntConcept;

    [Fact] void should_hold_zero() => result.Value.ShouldEqual(5);
}
