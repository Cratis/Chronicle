// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.given;

/// <summary>
/// Provides projection definitions and event schemas for runtime key normalization.
/// </summary>
public class a_projection_definition : Specification
{
    protected static readonly EventType _type = new("event-type", 1);
    protected static readonly EventType _otherType = new("other-event-type", 1);

    protected ProjectionDefinition _definition;
    protected ProjectionDefinition _result;
    protected IReadOnlyList<EventTypeSchema> _schemas;
    protected ICollection<EventType> _rewritten;

    void Establish()
    {
        _schemas = [Schema(_type, "name"), Schema(_otherType, "name")];
        _rewritten = new List<EventType>();
    }

    protected static EventTypeSchema Schema(EventType type, params string[] properties)
    {
        var members = new JsonObject();
        foreach (var property in properties)
        {
            members[property] = new JsonObject { ["type"] = "string" };
        }

        return new(type, EventTypeOwner.Client, EventTypeSource.User, JsonSchema.FromJson(new JsonObject
        {
            ["type"] = "object",
            ["properties"] = members
        }.ToJsonString()));
    }

    protected static FromDefinition From(string key = "EventSourceId", string? parentKey = null) =>
        new(new Dictionary<PropertyPath, string>(), key, parentKey is null ? null : new PropertyExpression(parentKey));

    protected static ChildrenDefinition Child(
        IDictionary<EventType, FromDefinition>? from = null,
        IDictionary<EventType, JoinDefinition>? join = null,
        IDictionary<EventType, RemovedWithDefinition>? removedWith = null,
        IDictionary<PropertyPath, ChildrenDefinition>? children = null,
        IDictionary<PropertyPath, ChildrenDefinition>? nested = null) => new(
            "id",
            from ?? new Dictionary<EventType, FromDefinition>(),
            join ?? new Dictionary<EventType, JoinDefinition>(),
            children ?? new Dictionary<PropertyPath, ChildrenDefinition>(),
            new(new Dictionary<PropertyPath, string>(), false),
            removedWith ?? new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Nested: nested);

    protected static ProjectionDefinition Definition(
        IDictionary<EventType, FromDefinition>? from = null,
        IDictionary<EventType, JoinDefinition>? join = null,
        IDictionary<EventType, RemovedWithDefinition>? removedWith = null,
        IDictionary<EventType, RemovedWithJoinDefinition>? removedWithJoin = null,
        IDictionary<PropertyPath, ChildrenDefinition>? children = null,
        IDictionary<PropertyPath, ChildrenDefinition>? nested = null,
        IEnumerable<FromDerivatives>? fromDerivatives = null,
        string fromEveryKey = "$eventSourceId") => new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "legacy-keys",
            "legacy-model",
            true,
            true,
            new(),
            from ?? new Dictionary<EventType, FromDefinition>(),
            join ?? new Dictionary<EventType, JoinDefinition>(),
            children ?? new Dictionary<PropertyPath, ChildrenDefinition>(),
            fromDerivatives ?? [],
            new(new Dictionary<PropertyPath, string>(), false) { Key = fromEveryKey },
            removedWith ?? new Dictionary<EventType, RemovedWithDefinition>(),
            removedWithJoin ?? new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Nested: nested);
}
