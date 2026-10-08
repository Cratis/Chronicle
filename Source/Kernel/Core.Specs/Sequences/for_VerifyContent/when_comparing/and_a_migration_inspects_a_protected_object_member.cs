// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_inspects_a_protected_object_member : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        foreach (var schema in new[] { _first, _second })
        {
            var container = schema.Properties["name"];
            container.Type = JsonObjectType.Object;
            container.Properties["first"] = new JsonSchemaProperty("first", new JsonObject(), schema) { Type = JsonObjectType.String };
        }

        _command = _command with { Content = """{"name":{"first":"a"},"kind":"x"}""" };
        MigrateWith("""{"note":{"$split":{"source":"name.first","separator":"=","part":0}}}""");
        Store(
            new JsonObject { ["name"] = Protect("""{"first":"a"}"""), ["kind"] = "x" },
            new JsonObject { ["name"] = Protect("""{"first":"a"}"""), ["kind"] = "x", ["note"] = "a" });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_reject_reading_inside_the_protected_boundary() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
