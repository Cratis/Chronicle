// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_datetime_concept_with_value_as_string_local_with_timezone : Specification
{
    DateTimeConcept result;
    string expected_as_string;
    DateTime expected;

    void Establish()
    {
        expected_as_string = "2022-12-14T09:45:46.4595800+01:00";
        expected = DateTime.Parse(expected_as_string);
    }

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DateTimeConcept), expected_as_string) as DateTimeConcept;

    [Fact] void should_be_the_value_of_the_datetime() => result.Value.ShouldEqual(expected);
}
