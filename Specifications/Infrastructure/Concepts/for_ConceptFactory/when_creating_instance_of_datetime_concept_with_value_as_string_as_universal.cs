// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_datetime_concept_with_value_as_string_as_universal : Specification
{
    DateTimeConcept result;
    string expected;

    void Establish() => expected = "2022-12-14T08:45:46.4595800Z";

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DateTimeConcept), expected) as DateTimeConcept;

    [Fact] void should_be_the_value_of_the_datetime() => result.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ").ShouldEqual(expected);
}
