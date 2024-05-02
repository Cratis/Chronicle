// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_date_only_concept_with_value_as_string : Specification
{
    DateOnlyConcept result;
    DateOnly input;
    string input_as_string;

    void Establish()
    {
        input = DateOnly.FromDateTime(DateTime.UtcNow);
        input_as_string = input.ToString("O");
    }

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DateOnlyConcept), input) as DateOnlyConcept;

    [Fact] void should_be_the_value_of_the_date_only() => result.Value.ShouldEqual(input);
}
