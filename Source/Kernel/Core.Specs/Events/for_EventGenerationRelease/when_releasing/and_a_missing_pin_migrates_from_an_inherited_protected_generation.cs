// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_a_missing_pin_migrates_from_an_inherited_protected_generation : given.a_downcast_protected_token
{
    void Establish()
    {
        var content = _event.GenerationalContent[1];
        _event = _event with
        {
            Content = _converter.ToExpandoObject(JsonNode.Parse(content).AsObject(), _schemas[0]),
            Context = _event.Context with { EventType = _pin, AppendedGeneration = null },
            GenerationalContent = new Dictionary<int, string> { [1] = content }
        };
        _pin = new(_eventType.Id, 2);
        _pinnedSchemas = new Dictionary<Concepts.Events.EventType, Concepts.EventTypes.EventTypeSchema> { [_pin] = new(_pin, Concepts.Events.EventTypeOwner.Server, Concepts.Events.EventTypeSource.Code, _schemas[1]) };
    }

    async Task Because() => _result = await Release();

    [Fact] void should_release_before_migrating() => ReleasedToken.ShouldEqual(Token);
    [Fact] void should_not_persist_delivery_time_content() => _result[0].GenerationalContent.ShouldBeSame(_event.GenerationalContent);
}
