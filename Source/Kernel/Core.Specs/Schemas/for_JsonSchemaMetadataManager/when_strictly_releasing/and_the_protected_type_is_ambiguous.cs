// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_the_protected_type_is_ambiguous : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":["number","string"]}}} """);
        Mark(_schema.Properties["value"]);
        _plaintext = new JsonObject { ["value"] = 42 };
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_not_guess_whether_the_plaintext_was_numeric_text_or_a_number() => _result.ShouldBeNull();
}
