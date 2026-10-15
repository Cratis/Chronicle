// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_conversion_of_a_migration_output_loses_precision : given.a_migrated_event
{
    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"number"}}}""");
        _storage.GetEventStore("store").EventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, schema));
        _definition = _definition with
        {
            Generations = [new(1, _definition.Generations.First().Schema), new(2, schema)],
            Migrations = [new(1, 2, [], JsonNode.Parse("""{"value":{"$mapValues":{"source":"value","mappings":[{"from":42,"to":0.10000000000000001}]}}}""")!.AsObject(), new JsonObject())]
        };
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = "{\"value\":42}", [2] = "{\"value\":0.1}" } };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_check_the_raw_migration_output_before_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
