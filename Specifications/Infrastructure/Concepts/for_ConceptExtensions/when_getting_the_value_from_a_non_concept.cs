// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptExtensions;

public class when_getting_the_value_from_a_non_concept : given.concepts
{
    static string primitive_value = "ten";
    static Exception exception;

    void Because() => exception = Catch.Exception(() => primitive_value.GetConceptValue());

    [Fact] void should_throw_an_argument_exception() => exception.ShouldBeOfExactType<TypeIsNotAConcept>();
}
