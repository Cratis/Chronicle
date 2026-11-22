// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_CanonicalJson;

public class when_selecting_the_direct_value_writer : Specification
{
    [Fact] void should_accept_ordinary_properties_and_lists() => CanonicalJsonValues.CanWrite(new Dictionary<string, object?> { ["list"] = new List<object?> { 1, "text", null } }).ShouldBeTrue();
    [Fact] void should_accept_expando_arrays() => CanonicalJsonValues.CanWrite(new[] { new ExpandoObject() }).ShouldBeTrue();
    [Fact] void should_accept_supplementary_names() => CanonicalJsonValues.CanWrite(new Dictionary<string, object?> { ["🚀"] = "🚀", ["\uE000"] = true }).ShouldBeTrue();
    [Fact] void should_keep_invalid_names_on_the_normalizing_path() => CanonicalJsonValues.CanWrite(new Dictionary<string, object?> { ["\uD800"] = 1 }).ShouldBeFalse();
    [Fact] void should_keep_unpaired_high_surrogates_on_the_normalizing_path() => CanonicalJsonValues.CanWrite("\uD800").ShouldBeFalse();
    [Fact] void should_keep_unpaired_low_surrogates_on_the_normalizing_path() => CanonicalJsonValues.CanWrite("\uDC00").ShouldBeFalse();
    [Fact] void should_keep_invalid_characters_on_the_normalizing_path() => CanonicalJsonValues.CanWrite('\uD800').ShouldBeFalse();
    [Fact] void should_keep_raw_json_numbers_on_the_serialized_path() => CanonicalJsonValues.CanWrite(JsonElement.Parse("1.2300e+2")).ShouldBeFalse();
    [Fact] void should_keep_json_nodes_on_the_serialized_path() => CanonicalJsonValues.CanWrite(JsonNode.Parse("{\"a\":1}")).ShouldBeFalse();
    [Fact] void should_keep_big_integer_objects_on_the_serialized_path() => CanonicalJsonValues.CanWrite(BigInteger.One).ShouldBeFalse();
    [Fact] void should_keep_enums_and_their_converters_on_the_serialized_path() => CanonicalJsonValues.CanWrite(DayOfWeek.Friday).ShouldBeFalse();
    [Fact] void should_keep_non_finite_doubles_on_the_serialized_path() => CanonicalJsonValues.CanWrite(double.NaN).ShouldBeFalse();
    [Fact] void should_keep_non_finite_floats_on_the_serialized_path() => CanonicalJsonValues.CanWrite(float.PositiveInfinity).ShouldBeFalse();
    [Fact] void should_keep_non_finite_halves_on_the_serialized_path() => CanonicalJsonValues.CanWrite(Half.NegativeInfinity).ShouldBeFalse();
    [Fact] void should_keep_typed_interface_arrays_on_the_serialized_path() => CanonicalJsonValues.CanWrite(new IEnumerable<KeyValuePair<string, object?>>[] { new ExpandoObject() }).ShouldBeFalse();

    [Fact]
    void should_preserve_duplicate_dictionary_names_with_stable_sorting()
    {
        var properties = new Dictionary<string, object?>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < 24; index++) properties[new string('a', 1)] = index;
        CanonicalJsonValues.CanWrite(properties).ShouldBeTrue();
        var expectedProperties = string.Join(',', Enumerable.Range(0, 24).Select(index => $"\"a\":{index}"));
        CanonicalJson.Encode(properties).ShouldEqual($"{{{expectedProperties}}}");
    }

    [Fact]
    void should_keep_cycles_on_the_serialized_path()
    {
        var cycle = new List<object?>();
        cycle.Add(cycle);
        CanonicalJsonValues.CanWrite(cycle).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_values_at_the_serializer_depth_boundary()
    {
        object? value = 1;
        for (var depth = 0; depth < 63; depth++) value = new object?[] { value };
        CanonicalJsonValues.CanWrite(value).ShouldBeTrue();
        CanonicalJson.Encode(value).ShouldEqual($"{new string('[', 63)}1{new string(']', 63)}");
    }

    [Fact]
    void should_keep_deep_graphs_on_the_serialized_path()
    {
        object? value = null;
        for (var depth = 0; depth < 65; depth++) value = new object?[] { value };
        CanonicalJsonValues.CanWrite(value).ShouldBeFalse();
    }
}
