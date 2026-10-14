// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_content_is_encrypted : given.a_replay
{
    void Establish()
    {
        var schema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"email\":{\"type\":\"string\"}}}");
        schema.Properties["email"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };
        _types.Configure().GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(new("Claimed", 1), EventTypeOwner.None, EventTypeSource.Code, schema));
        var plaintext = new ExpandoObject();
        ((IDictionary<string, object?>)plaintext)["email"] = "plaintext";
        _converter.ToJsonObject(Arg.Any<ExpandoObject>(), schema).Returns(new JsonObject { ["email"] = "ciphertext" });
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), schema, Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(new JsonObject { ["email"] = "plaintext" });
        _converter.ToExpandoObject(Arg.Any<JsonObject>(), schema).Returns(plaintext);
        Event(0, "source", "Claimed", "ciphertext");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_hash_the_plaintext_like_the_append_path() => (await Values("source")).ShouldContainOnly(Hash("plaintext").Value);
    [Fact] async Task should_not_hash_the_ciphertext() => (await Values("source")).ShouldNotContain(Hash("ciphertext").Value);
}
