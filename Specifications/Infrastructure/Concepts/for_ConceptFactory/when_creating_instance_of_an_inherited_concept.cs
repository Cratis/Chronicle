// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_an_inherited_concept : Specification
{
    const long long_value = 42;

    InheritedConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(InheritedConcept), long_value) as InheritedConcept;

    [Fact] void should_hold_the_correct_long_value() => result.Value.ShouldEqual(long_value);
}
