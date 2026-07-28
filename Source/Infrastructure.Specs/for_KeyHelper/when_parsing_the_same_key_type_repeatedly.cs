// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_KeyHelper;

public class when_parsing_the_same_key_type_repeatedly : Specification
{
    record SomeConcept(string Value) : ConceptAs<string>(Value);

    record Key(SomeConcept First, string Second, int Third);

    string _key;
    Key _firstResult;
    Key _secondResult;

    void Establish() => _key = KeyHelper.Combine("first", "second", "3");

    void Because()
    {
        _firstResult = KeyHelper.Parse<Key>(_key);
        _secondResult = KeyHelper.Parse<Key>(_key);
    }

    [Fact] public void should_resolve_the_concept_parameter() => _firstResult.First.Value.ShouldEqual("first");
    [Fact] public void should_resolve_the_string_parameter() => _firstResult.Second.ShouldEqual("second");
    [Fact] public void should_convert_the_value_type_parameter() => _firstResult.Third.ShouldEqual(3);
    [Fact] public void should_return_an_equal_result_when_the_signature_is_cached() => _secondResult.ShouldEqual(_firstResult);
}
