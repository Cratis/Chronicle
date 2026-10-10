// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_a_scalar_for_an_object_property : Specification
{
    JsonObject _result;

    void Because()
    {
        dynamic content = new ExpandoObject();
        content.id = "6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11_0";
        content.name = "composite";
        _result = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject((ExpandoObject)content,
            JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"uint64"}}},"name":{"type":"string"}}}"""));
    }

    [Fact] void should_not_emit_the_mismatched_value() => _result["id"].ShouldBeNull();
    [Fact] void should_keep_the_other_properties() => _result["name"]!.GetValue<string>().ShouldEqual("composite");
}
