// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_concept_with_null_value : Specification
{
    Exception result;

    void Because() => result = Catch.Exception(() => ConceptFactory.CreateConceptInstance(typeof(IntConcept), null));

    [Fact] void should_throw_argument_null_exception() => result.ShouldBeOfExactType<ArgumentNullException>();
}
