// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptExtensions;

public class when_checking_is_concept_on_a_concept_instance : given.concepts
{
    static bool is_a_concept;

    void Because() => is_a_concept = value_as_a_long.IsConcept();

    [Fact] void should_be_a_concept() => is_a_concept.ShouldBeTrue();
}
