// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_a_schema_property_has_no_value;

/// <summary>
/// The non-goal of withholding illegal defaults, stated as a spec. What disqualifies a synthesized value is that
/// the property's own member list does not contain it - not that the property happens to be an enum. An enum that
/// declares <c>NotSet = 0</c> has said that zero is a real answer, and the round trip must keep writing it: the
/// member list is consulted, not bypassed, and it says yes.
/// <para>
/// Without this, the membership test is a branch every subject in the suite takes the same way. Every other enum
/// in the fixture is 1-based, so replacing the test with a flat "not a declared member" leaves the whole suite
/// green and quietly turns a legal default into an absent field - the mirror-image defect, in which a read model
/// that answered <c>NotSet</c> comes back as one that did not answer at all.
/// </para>
/// <para>
/// A member list that names its members is written by name, so what lands on the wire is <c>"NotSet"</c> and
/// what comes back is the <c>0</c> behind it.
/// </para>
/// </summary>
public class and_it_is_a_non_nullable_enum_with_a_zero_member : given.an_expando_object_converter_with_a_read_model_schema
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

    [Fact] void should_write_the_declared_zero_member() => _asJson["decision"]!.GetValue<string>().ShouldEqual("NotSet");
    [Fact] void should_materialize_the_declared_zero_member_on_the_way_back() => _roundTripped["decision"].ShouldEqual(0);
}
