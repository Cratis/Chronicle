// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_a_stale_registration_can_still_upcast : given.a_stored_chain
{
    IDictionary<EventTypeGeneration, ExpandoObject> _result;

    async Task Because()
    {
        await Register();
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var engine = new EventTypeMigrations(_storage, converter);
        var content = new JsonObject { ["one"] = "payload" };
        _result = await engine.MigrateToAllGenerations("test-store", new(_chain.Id, 1), content, converter.ToExpandoObject(content, _chain.Generations.First().Schema));
    }

    [Fact] void should_upcast_across_both_retained_bridges() => ((IDictionary<string, object?>)_result[(EventTypeGeneration)3])["three"].ShouldEqual("payload");
    [Fact] void should_keep_the_intermediate_generation_value() => ((IDictionary<string, object?>)_result[(EventTypeGeneration)2])["two"].ShouldEqual("payload");
    [Fact] void should_return_all_generations() => _result.Count.ShouldEqual(3);
}
