// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_a_format_is_unsupported : Specification
{
    Dictionary<string, Exception> _errors;

    void Because()
    {
        var inputs = new Dictionary<string, UniqueConstraintComponent>
        {
            ["byte-array"] = new(new byte[] { 1 }, "byte-array"),
            ["point"] = new(null, "point"),
            ["linestring"] = new(null, "linestring"),
            ["polygon"] = new(null, "polygon"),
            ["object"] = new(null, "object"),
            ["dynamic"] = new(null, "dynamic"),
            ["unknown-format"] = new("value", "unrecognized"),
            ["object-value"] = new(new object()),
            ["dictionary"] = new(new Dictionary<string, object> { ["key"] = "value" }, "dynamic"),
            ["nested-array"] = new(new[] { new[] { "value" } }),
            ["empty-nested-array"] = new(Array.Empty<string[]>()),
            ["null-nested-array-item"] = new(new string[]?[] { null }),
            ["multidimensional-array"] = new(new string[1, 1]),
            ["nested-object-array"] = new(new object[] { new[] { "value" } }),
            ["array-object"] = new(new[] { new object() }),
            ["array-byte-array-format"] = new(Array.Empty<byte[]>(), "byte-array")
        };
        _errors = inputs.ToDictionary(_ => _.Key, _ => Catch.Exception(() => { UniqueConstraintEncoder.Encode([1], [_.Value]); }));
    }

    [Fact] void should_reject_byte_arrays() => _errors["byte-array"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_points_even_when_null() => _errors["point"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_linestrings_even_when_null() => _errors["linestring"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_polygons_even_when_null() => _errors["polygon"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_objects_even_when_null() => _errors["object"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_dynamic_even_when_null() => _errors["dynamic"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_unrecognized_formats() => _errors["unknown-format"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_object_shaped_values_without_a_format() => _errors["object-value"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_dictionaries() => _errors["dictionary"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_nested_arrays() => _errors["nested-array"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_empty_nested_arrays() => _errors["empty-nested-array"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_null_nested_array_items() => _errors["null-nested-array-item"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_multidimensional_arrays() => _errors["multidimensional-array"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_arrays_hidden_in_object_arrays() => _errors["nested-object-array"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_object_array_leaves() => _errors["array-object"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
    [Fact] void should_reject_unsupported_element_formats_even_when_empty() => _errors["array-byte-array-format"].ShouldBeOfExactType<UnsupportedUniqueConstraintFormat>();
}
