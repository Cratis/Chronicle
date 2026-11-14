// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_splits_an_unprotected_collection_element : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        _first = CollectionSchema(false);
        _second = CollectionSchema(true);
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, _first));
        eventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, _second));
        _command = _command with { Content = """{"items":[{"secret":"a","name":"n=m"}]}""" };
        MigrateWith("""{"items[].derived":{"$split":{"source":"items[].name","separator":"=","part":0}}}""");
        Store(
            new JsonObject { ["items"] = new JsonArray(new JsonObject { ["secret"] = Protect("a"), ["name"] = "n=m" }) },
            new JsonObject { ["items"] = new JsonArray(new JsonObject { ["secret"] = Protect("a"), ["name"] = "n=m", ["derived"] = "n" }) });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_reach_the_comparison() => _result.Result.ShouldNotEqual(ContentVerificationResult.Unavailable);

    static JsonSchema CollectionSchema(bool withDerived)
    {
        var derived = withDerived ? ",\"derived\":{\"type\":\"string\"}" : string.Empty;
        var json = "{\"type\":\"object\",\"properties\":{\"items\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"properties\":{\"secret\":{\"type\":\"string\",\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]},\"name\":{\"type\":\"string\"}" + derived + "}}}}}";
        return JsonSchema.FromJsonAsync(json).GetAwaiter().GetResult();
    }
}
