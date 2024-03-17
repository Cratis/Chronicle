// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptMap;

public class when_getting_the_primitive_type_from_a_type_that_is_not_a_concept : Specification
{
    static Type result;

    void Because() => result = ConceptMap.GetConceptValueType(typeof(string));

    [Fact] void should_get_void_type() => (result == typeof(void)).ShouldBeTrue();
}
