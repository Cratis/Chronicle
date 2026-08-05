// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_a_schema_property_has_no_value;

/// <summary>
/// Nullability is not what makes the synthesized value illegal - the member list is. A non-nullable enum that
/// starts at 1 has no member to absorb a <c>0</c> either, and 1-basing is exactly what a consumer is pushed
/// towards by the opposite defect, where an explicitly projected zero-valued enum is written as an absent field.
/// Between the two there is no safe numbering, so the synthesis has to stop producing values outside the
/// property's own declared set regardless of how the property is declared.
/// </summary>
public class and_it_is_a_non_nullable_enum_with_no_zero_member : given.an_expando_object_converter_with_a_read_model_schema
{
    JsonObject _asJson;
    IDictionary<string, object?> _roundTripped;

    void Because()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["id"] = "the-contract";
        _asJson = converter.ToJsonObject(state, schema);
        _roundTripped = converter.ToExpandoObject(_asJson, schema);
    }

    [Fact] void should_not_write_a_value_outside_the_declared_members() => _asJson.ContainsKey("status").ShouldBeFalse();
    [Fact] void should_not_materialize_it_on_the_way_back() => _roundTripped.ContainsKey("status").ShouldBeFalse();
    [Fact] void should_not_default_a_nullable_flag_to_false() => _asJson.ContainsKey("isFinalized").ShouldBeFalse();
    [Fact] void should_not_materialize_the_nullable_flag_on_the_way_back() => _roundTripped.ContainsKey("isFinalized").ShouldBeFalse();
    [Fact] void should_still_default_a_non_nullable_number() => _asJson["rate"]!.GetValue<decimal>().ShouldEqual(0m);
    [Fact] void should_still_default_the_non_nullable_number_on_the_way_back() => _roundTripped["rate"].ShouldEqual(0m);
}
