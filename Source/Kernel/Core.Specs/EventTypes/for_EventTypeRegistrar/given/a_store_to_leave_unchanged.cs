// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class a_store_to_leave_unchanged : a_stored_chain
{
    protected EventTypeRegistration _newEventType;
    protected EventTypeRegistration _newGeneration;
    protected EventTypeRegistration _rejected;

    void Establish()
    {
        const string FourthSchema = """{"type":"object","properties":{"four":{"type":"string"}}}""";
        const string ChangedSecondSchema = """{"type":"object","properties":{"two":{"type":"integer"}}}""";

        _newEventType = new() { Type = new() { Id = "brand-new", Generation = 1 }, Schema = FirstSchema };
        _newGeneration = new()
        {
            Type = new() { Id = _chain.Id.Value, Generation = 4 },
            Schema = FourthSchema,
            Generations =
            [
                new() { Generation = 1, Schema = FirstSchema },
                new() { Generation = 2, Schema = SecondSchema },
                new() { Generation = 3, Schema = ThirdSchema },
                new() { Generation = 4, Schema = FourthSchema }
            ],
            Migrations =
            [
                new() { FromGeneration = 1, ToGeneration = 2, UpcastJmesPath = "{\"two\":\"one\"}", DowncastJmesPath = "{\"one\":\"two\"}" },
                new() { FromGeneration = 2, ToGeneration = 3, UpcastJmesPath = "{\"three\":\"two\"}", DowncastJmesPath = "{\"two\":\"three\"}" },
                new() { FromGeneration = 3, ToGeneration = 4, UpcastJmesPath = "{\"four\":\"three\"}", DowncastJmesPath = "{\"three\":\"four\"}" }
            ]
        };
        _rejected = new()
        {
            Type = new() { Id = _chain.Id.Value, Generation = 2 },
            Schema = ChangedSecondSchema,
            Generations = [new() { Generation = 1, Schema = FirstSchema }, new() { Generation = 2, Schema = ChangedSecondSchema }],
            Migrations = _request.Migrations
        };
    }

    protected async Task<string> Snapshot()
    {
        var definitions = await _memory.GetAllDefinitions();
        return string.Join('|', definitions.OrderBy(_ => _.Id.Value).Select(definition =>
            $"{definition.Id.Value}:{string.Join(',', definition.Generations.OrderBy(_ => _.Generation.Value).Select(_ => $"{_.Generation.Value}={_.Schema.ToJson()}"))}:{definition.Migrations.Count()}"));
    }
}
