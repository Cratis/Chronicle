// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory
{
    public class when_creating_instance_of_guid_concept_from_a_byte_array : Specification
    {
        const string guid_value_as_string = "4AB92720-3138-4A7B-A7E9-2A49F6624736";
        Guid guid;
        GuidConcept result;

        void Establish() => guid = Guid.Parse(guid_value_as_string);

        void Because() => result = ConceptFactory.CreateConceptInstance(typeof(GuidConcept), guid.ToByteArray()) as GuidConcept;

        [Fact] void should_hold_the_correct_guid_value() => result.Value.ToString().ToUpperInvariant().ShouldEqual(guid_value_as_string);
    }
}
