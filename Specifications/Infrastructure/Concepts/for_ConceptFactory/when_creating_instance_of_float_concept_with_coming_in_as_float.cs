// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_float_concept_with_coming_in_as_float : Specification
{
    FloatConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(FloatConcept), 5f) as FloatConcept;

    [Fact] void should_hold_zero() => result.Value.ShouldEqual(5f);
}
