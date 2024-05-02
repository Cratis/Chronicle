// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_long_concept_with_value_as_string : Specification
{
    const string long_value_as_string = "42";

    LongConcept result;

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(LongConcept), long_value_as_string) as LongConcept;

    [Fact] void should_hold_the_correct_long_value() => result.Value.ToString().ShouldEqual(long_value_as_string);
}
