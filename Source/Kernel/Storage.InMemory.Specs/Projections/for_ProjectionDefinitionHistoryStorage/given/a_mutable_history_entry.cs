// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionHistoryStorage.given;

public class a_mutable_history_entry : a_history_storage
{
    protected static readonly EventType _first = new("event", 1);
    protected static readonly EventType _second = new("event", 2);

    void Establish()
    {
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<EventType, FromDefinition> { [_first] = From() },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new(new Dictionary<PropertyPath, string> { ["name"] = "original" }, false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());

        // Concrete arrays on purpose: Mutate writes through them to prove the stored snapshot is detached.
        EventType[] derivativeEventTypes = [_first, _second];
        string[] tags = ["original"];
        PropertyPath[] excludedPaths = ["original"];
        _entry = _entry with
        {
            Definition = _entry.Definition with
            {
                InitialModelState = new JsonObject { ["nested"] = new JsonObject { ["value"] = "original" } },
                From = new Dictionary<EventType, FromDefinition> { [_first] = From(), [_second] = From() },
                Join = new Dictionary<EventType, JoinDefinition> { [_first] = new("id", new Dictionary<PropertyPath, string> { ["name"] = "original" }, "$eventSourceId") },
                Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = child },
                Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["nested"] = child },
                FromDerivatives = [new(derivativeEventTypes, From())],
                FromEvery = new(new Dictionary<PropertyPath, string> { ["name"] = "original" }, false),
                Tags = tags,
                NoAutoMapProperties = excludedPaths
            }
        };
    }

    protected static void Mutate(ProjectionDefinition definition)
    {
        definition.InitialModelState["nested"]!["value"] = "changed";
        definition.From[_first].Properties["name"] = "changed";
        definition.From.Remove(_second);
        definition.Join[_first].Properties["name"] = "changed";
        definition.Children["children"].From[_first].Properties["name"] = "changed";
        definition.Children["children"].All.Properties["name"] = "changed";
        definition.Nested!["nested"].FromEvery.Properties["name"] = "changed";
        definition.FromDerivatives.Single().From.Properties["name"] = "changed";
        ((EventType[])definition.FromDerivatives.Single().EventTypes)[0] = new("changed", 3);
        definition.FromEvery.Properties["name"] = "changed";
        definition.FromEvery.Key = "changed";
        ((string[])definition.Tags!)[0] = "changed";
        ((PropertyPath[])definition.NoAutoMapProperties!)[0] = "changed";
    }

    static FromDefinition From() => new(new Dictionary<PropertyPath, string> { ["name"] = "original" }, "$eventSourceId", null);
}
