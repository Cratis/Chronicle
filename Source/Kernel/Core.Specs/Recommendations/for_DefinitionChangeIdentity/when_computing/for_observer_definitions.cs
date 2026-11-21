// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Recommendations.for_DefinitionChangeIdentity.when_computing;

public class for_observer_definitions : Specification
{
    [Theory]
    [InlineData("projection")]
    [InlineData("reducer")]
    [InlineData("reactor")]
    [InlineData("webhook")]
    void should_hash_equal_definitions_equally(string observerType)
    {
        var (definition, equal, changed) = Definitions(observerType);
        var result = DefinitionChangeIdentity.For(definition);
        result.ShouldEqual(DefinitionChangeIdentity.For(equal));
        result.ShouldEqual(DefinitionChangeIdentity.For(definition));
        result.ShouldNotEqual(DefinitionChangeIdentity.For(changed));
        result.Length.ShouldEqual(64);
        result.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f').ShouldBeTrue();
    }

    static (object Definition, object Equal, object Changed) Definitions(string observerType) => observerType switch
    {
        "projection" => (Projection(), Projection(), Projection() with { IsActive = false }),
        "reducer" => (Reducer(), Reducer(), Reducer() with { Hash = "changed" }),
        "reactor" => (Reactor(), Reactor(), Reactor() with { IsReplayable = false }),
        "webhook" => (Webhook(), Webhook(), Webhook() with { IsActive = false }),
        _ => throw new ArgumentOutOfRangeException(nameof(observerType))
    };

    static ProjectionDefinition Projection() => new(
        ProjectionOwner.Client,
        "log",
        "projection",
        "read-model",
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition>
        {
            [new("event", 1)] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null)
        },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    static ReducerDefinition Reducer() => new("reducer", "log", [], "read-model", true, []);
    static ReactorDefinition Reactor() => new("reactor", ReactorOwner.Client, "log", []);
    static WebhookDefinition Webhook() => new("webhook", WebhookOwner.Client, "log", [], new(new("https://example.com"), WebhookAuthorization.None, new Dictionary<string, string>()));
}
