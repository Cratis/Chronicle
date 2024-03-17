// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS1718

namespace Cratis.Concepts.for_ConceptAs;

public class when_constructing_with_null_value : Specification
{
    record RefConcept(string Value) : ConceptAs<string>(Value);

    Exception result;

    void Because() => result = Catch.Exception(() => { var _ = new RefConcept(null); });

    [Fact] void should_throw_argument_null_exception() => result.ShouldBeOfExactType<ArgumentNullException>();
}
