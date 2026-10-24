// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_values_have_other_types : Specification
{
    static readonly Guid _guid = Guid.Parse("0D0B9D4C-6E0A-4C4F-8C1B-2B6E3F6B1A11");
    string _guidResult;
    string _enumResult;
    string _conceptResult;
    string _plainResult;
    string _boolResult;
    string _integerResult;

    void Because()
    {
        _guidResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = _guid });
        _enumResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = SomeKind.Second });
        _conceptResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = new SomeConcept("abc") });
        _plainResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = "abc" });
        _boolResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = true, ["b"] = false });
        _integerResult = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = 42L, ["b"] = -3 });
    }

    [Fact] void should_write_a_guid_in_lowercase_d_format() => _guidResult.ShouldEqual("~k1:36:0d0b9d4c-6e0a-4c4f-8c1b-2b6e3f6b1a11");
    [Fact] void should_write_an_enum_as_its_number() => _enumResult.ShouldEqual("~k1:1:7");
    [Fact] void should_write_a_concept_as_the_value_it_holds() => _conceptResult.ShouldEqual(_plainResult);
    [Fact] void should_write_booleans_in_lowercase() => _boolResult.ShouldEqual("~k1:4:true5:false");
    [Fact] void should_write_integers_invariantly() => _integerResult.ShouldEqual("~k1:2:422:-3");
}
