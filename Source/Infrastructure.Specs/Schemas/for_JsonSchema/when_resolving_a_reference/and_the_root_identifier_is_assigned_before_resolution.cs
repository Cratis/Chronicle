// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class and_the_root_identifier_is_assigned_before_resolution : Specification
{
    JsonSchema _schema;
    JsonSchema _resolved;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "properties":{"value":{"$ref":"personal"}},
              "$defs":{"personal":{"$id":"https://example.invalid/personal","title":"personal"}}
            }
            """);
        _schema.ExtensionData!["$id"] = "https://example.invalid/";
    }

    void Because() => _resolved = _schema.Properties["value"].Reference;

    [Fact] void should_use_the_identifier_when_first_resolving() => _resolved?.Title.ShouldEqual("personal");
}
