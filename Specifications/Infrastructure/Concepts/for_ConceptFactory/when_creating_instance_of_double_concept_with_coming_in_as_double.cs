// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_double_concept_with_coming_in_as_double : Specification
{
    DoubleConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DoubleConcept), 5d) as DoubleConcept;

    [Fact] void should_hold_the_double() => result.Value.ShouldEqual(5d);
}
