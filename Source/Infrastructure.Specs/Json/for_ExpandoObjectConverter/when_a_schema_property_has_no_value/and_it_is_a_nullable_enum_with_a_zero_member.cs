// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_a_schema_property_has_no_value;

/// <summary>
/// Two independent guards decide this, and every other subject in the fixture is suppressed by both of them at
/// once - so each of those specs stays green with either guard removed, and the pair is only ever observed as a
/// pair. This property is the one a member list cannot account for: a nullable enum that <em>does</em> declare a
/// zero member, so <c>0</c> is a value its own schema permits. Nothing but the nullability reading keeps it out.
/// <para>
/// The reading in question is the type-array one. An enum carries no <c>format</c>, so the trailing <c>?</c> the
/// converter used to test for is not where its nullability lives - it lives in <c>"type": ["integer", "null"]</c>.
/// Drop that leg and this property, alone among the fixture's, starts materializing a <c>0</c> that reads like a
/// deliberate <c>NotSet</c> answer on a read model that never answered.
/// </para>
/// </summary>
public class and_it_is_a_nullable_enum_with_a_zero_member : given.an_expando_object_converter_with_a_read_model_schema
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

    [Fact] void should_not_answer_for_a_read_model_that_never_answered() => _asJson.ContainsKey("feedback").ShouldBeFalse();
    [Fact] void should_not_materialize_it_on_the_way_back() => _roundTripped.ContainsKey("feedback").ShouldBeFalse();
}
