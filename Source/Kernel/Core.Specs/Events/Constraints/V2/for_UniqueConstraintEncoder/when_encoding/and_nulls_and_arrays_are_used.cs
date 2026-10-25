// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_nulls_and_arrays_are_used : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    Dictionary<string, Option<UniqueConstraintValue>> _results;

    void Because()
    {
        var inputs = new Dictionary<string, UniqueConstraintComponent[]>
        {
            ["empty-tuple"] = [],
            ["null"] = [new(Value: null)],
            ["all-null"] = [new(Value: null), new(Value: null)],
            ["empty-string"] = [new("")],
            ["empty-array"] = [new(Array.Empty<string>())],
            ["null-array-items"] = [new(new string?[] { null, null })],
            ["array-empty-item"] = [new(new[] { "" })],
            ["left-null"] = [new(Value: null), new("x")],
            ["right-null"] = [new("x"), new(Value: null)],
            ["one-scalar"] = [new("x")],
            ["one-array-item"] = [new(new[] { "x" })],
            ["array-left-null"] = [new(new string?[] { null, "x" })],
            ["array-right-null"] = [new(new string?[] { "x", null })],
            ["array-forward"] = [new(new[] { "a", "b" })],
            ["array-reverse"] = [new(new[] { "b", "a" })],
            ["array-repeated"] = [new(new[] { "a", "a" })],
            ["array-once"] = [new(new[] { "a" })],
            ["array-integers"] = [new(new[] { 1, 2 }, "int32")],
            ["array-strings"] = [new(new[] { "1", "2" })],
            ["array-booleans"] = [new(new[] { true, false })],
            ["array-boolean-strings"] = [new(new[] { "true", "false" })],
            ["array-guid"] = [new(new[] { new Guid("abcdef01-2345-6789-abcd-ef0123456789") }, "guid")],
            ["array-guid-string"] = [new(new[] { "abcdef01-2345-6789-abcd-ef0123456789" })],
            ["array-date"] = [new(new[] { new DateOnly(2025, 2, 3) }, "date")],
            ["array-date-string"] = [new(new[] { "2025-02-03" })]
        };
        _results = inputs.ToDictionary(_ => _.Key, _ => UniqueConstraintEncoder.Encode(_key, _.Value));
    }

    string Value(string name) => _results[name].AsT0.Value;

    [Fact] void should_not_claim_an_empty_tuple() => _results["empty-tuple"].HasValue.ShouldBeFalse();
    [Fact] void should_not_claim_a_null_component() => _results["null"].HasValue.ShouldBeFalse();
    [Fact] void should_not_claim_an_all_null_tuple() => _results["all-null"].HasValue.ShouldBeFalse();
    [Fact] void should_claim_an_empty_string() => _results["empty-string"].HasValue.ShouldBeTrue();
    [Fact] void should_claim_an_empty_array() => _results["empty-array"].HasValue.ShouldBeTrue();
    [Fact] void should_claim_a_present_array_of_nulls() => _results["null-array-items"].HasValue.ShouldBeTrue();
    [Fact] void should_distinguish_empty_array_and_empty_string() => Value("empty-array").ShouldNotEqual(Value("empty-string"));
    [Fact] void should_distinguish_empty_array_and_an_empty_item() => Value("empty-array").ShouldNotEqual(Value("array-empty-item"));
    [Fact] void should_distinguish_null_positions() => Value("left-null").ShouldNotEqual(Value("right-null"));
    [Fact] void should_not_pad_a_shorter_tuple() => Value("one-scalar").ShouldNotEqual(Value("right-null"));
    [Fact] void should_distinguish_array_and_scalar() => Value("one-array-item").ShouldNotEqual(Value("one-scalar"));
    [Fact] void should_distinguish_array_null_positions() => Value("array-left-null").ShouldNotEqual(Value("array-right-null"));
    [Fact] void should_preserve_array_order() => Value("array-forward").ShouldNotEqual(Value("array-reverse"));
    [Fact] void should_preserve_repeated_array_elements() => Value("array-repeated").ShouldNotEqual(Value("array-once"));
    [Fact] void should_canonicalize_integer_array_leaves_without_a_kind_byte() => Value("array-integers").ShouldEqual(Value("array-strings"));
    [Fact] void should_canonicalize_boolean_array_leaves() => Value("array-booleans").ShouldEqual(Value("array-boolean-strings"));
    [Fact] void should_canonicalize_guid_array_leaves() => Value("array-guid").ShouldEqual(Value("array-guid-string"));
    [Fact] void should_canonicalize_date_array_leaves() => Value("array-date").ShouldEqual(Value("array-date-string"));
}
