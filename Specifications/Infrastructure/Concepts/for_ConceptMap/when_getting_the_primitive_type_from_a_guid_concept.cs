// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts.given;

namespace Cratis.Concepts.for_ConceptMap;

public class when_getting_the_primitive_type_from_a_guid_concept : Specification
{
    static Type result;

    void Because() => result = ConceptMap.GetConceptValueType(typeof(GuidConcept));

    [Fact] void should_get_a_guid() => result.ShouldEqual(typeof(Guid));
}
