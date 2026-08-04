// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_a_schema_property_has_no_value;

/// <summary>
/// The compliance step round-trips a read model's whole state through this converter, and the converter walks the
/// <em>schema's</em> properties rather than the document's - so a property no event ever set is one the converter
/// has to decide about. Deciding it by materializing the CLR default writes a literal <c>0</c> for an enum, and a
/// projection replay then stamps that onto every stored record.
/// <para>
/// The value is not merely surprising, it is one the read model's own registered schema forbids: this property
/// declares <c>enum: [1, 2, 3, 4]</c>. A reader either refuses it - taking down a whole observable query rather
/// than one row - or silently rounds it off to a value that reads like a deliberate business answer. The kernel
/// must not write a value it has itself declared illegal.
/// </para>
/// <para>
/// The nullability marker the converter tested was a trailing <c>?</c> on the property's <c>format</c>, which an
/// enum does not carry: its nullability lives in the <c>type</c> array. So a nullable enum was treated as
/// non-nullable and defaulted, while a nullable decimal beside it was exempt.
/// </para>
/// </summary>
public class and_it_is_a_nullable_enum : given.an_expando_object_converter_with_a_read_model_schema
{
    JsonObject _asJson;
    ExpandoObject _roundTripped;

    void Because()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["id"] = "the-contract";

        _asJson = converter.ToJsonObject(state, schema);
        _roundTripped = converter.ToExpandoObject(_asJson, schema);
    }

    [Fact] void should_not_write_a_value_the_schema_forbids() => _asJson.ContainsKey("rejectionReason").ShouldBeFalse();
    [Fact] void should_not_materialize_it_on_the_way_back() => ((IDictionary<string, object?>)_roundTripped).ContainsKey("rejectionReason").ShouldBeFalse();
    [Fact] void should_keep_the_value_that_was_set() => _asJson["id"]!.GetValue<string>().ShouldEqual("the-contract");
}
