// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_generating_from_type;

public class for_a_simple_type : Specification
{
    record SimpleType(int AnInteger, string AString, Guid AnId);

    JsonSchema _result;

    void Because() => _result = JsonSchema.FromType<SimpleType>();

    [Fact] void should_set_title_to_type_name() => _result.Title.ShouldEqual(nameof(SimpleType));
    [Fact] void should_have_expected_properties() => _result.ActualProperties.Keys.ShouldContainOnly("anInteger", "aString", "anId");
    [Fact] void should_inject_guid_format_for_guid_property() => _result.ActualProperties["anId"].Format.ShouldEqual("guid");
    [Fact] void should_inject_int32_format_for_integer_property() => _result.ActualProperties["anInteger"].Format.ShouldEqual("int32");
    [Fact] void should_not_inject_format_for_string_property() => _result.ActualProperties["aString"].Format.ShouldBeNull();
}
