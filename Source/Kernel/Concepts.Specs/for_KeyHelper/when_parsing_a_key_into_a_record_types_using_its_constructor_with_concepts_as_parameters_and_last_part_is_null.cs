// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.for_KeyHelper;

public class when_parsing_a_key_into_a_record_types_using_its_constructor_with_concepts_as_parameters_and_last_part_is_null : Specification
{
    record SomeConcept(string Value) : ConceptAs<string>(Value);

    record Key(SomeConcept First, SomeConcept Second, SomeConcept Third);

    string[] _parts;
    string _key;
    Key _result;

    void Establish()
    {
        _parts = ["First", "Second", null!];
        _key = KeyHelper.Combine(_parts);
    }

    void Because() => _result = KeyHelper.Parse<Key>(_key);

    [Fact] public void should_create_a_key_from_the_parts() => _result.ShouldNotBeNull();
    [Fact] public void should_have_the_first_part() => _result.First.Value.ShouldEqual(_parts[0]);
    [Fact] public void should_have_the_second_part_as_null() => _result.Second.Value.ShouldEqual(_parts[1]);
    [Fact] public void should_have_the_third_part() => _result.Third.ShouldBeNull();
}
