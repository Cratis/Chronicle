// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_FencedGlobalEligibility.given;

public static class Definitions
{
    public static readonly EventType SomeEvent = new("SomeEvent", EventTypeGeneration.First);

    public static ProjectionDefinition Plain() => Root(Joins(false), Children(), Removals(false), Nested());

    public static ProjectionDefinition WithJoin() => Root(Joins(true), Children(), Removals(false), Nested());

    public static ProjectionDefinition WithChildren() => Root(Joins(false), Children(NestedChild()), Removals(false), Nested());

    public static ProjectionDefinition WithRemoval() => Root(Joins(false), Children(), Removals(true), Nested());

    public static ProjectionDefinition WithNestedJoin() => Root(Joins(false), Children(), Removals(false), Nested(NestedChild(joins: Joins(true))));

    public static ProjectionDefinition WithNestedRemoval() => Root(Joins(false), Children(), Removals(false), Nested(NestedChild(removals: Removals(true))));

    public static ProjectionDefinition WithChildrenTwoLevelsDeep() =>
        Root(Joins(false), Children(), Removals(false), Nested(NestedChild(nested: Nested(NestedChild(children: Children(NestedChild()))))));

    static ProjectionDefinition Root(
        IDictionary<EventType, JoinDefinition> joins,
        IDictionary<PropertyPath, ChildrenDefinition> children,
        IDictionary<EventType, RemovedWithDefinition> removals,
        IDictionary<PropertyPath, ChildrenDefinition> nested) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "Some.Projection",
            "Some.ReadModel",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            joins,
            children,
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            removals,
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Nested: nested,
            Scope: ProjectionScope.Global);

    static ChildrenDefinition NestedChild(
        IDictionary<EventType, JoinDefinition>? joins = null,
        IDictionary<EventType, RemovedWithDefinition>? removals = null,
        IDictionary<PropertyPath, ChildrenDefinition>? children = null,
        IDictionary<PropertyPath, ChildrenDefinition>? nested = null) =>
        new(
            "id",
            new Dictionary<EventType, FromDefinition>(),
            joins ?? Joins(false),
            children ?? Children(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            removals ?? Removals(false),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Nested: nested ?? Nested());

    static Dictionary<EventType, JoinDefinition> Joins(bool any) =>
        any ? new() { [SomeEvent] = new JoinDefinition("other", new Dictionary<PropertyPath, string>(), "key") } : [];

    static Dictionary<EventType, RemovedWithDefinition> Removals(bool any) =>
        any ? new() { [SomeEvent] = new RemovedWithDefinition("key", null) } : [];

    static Dictionary<PropertyPath, ChildrenDefinition> Children(params ChildrenDefinition[] children) =>
        children.ToDictionary(_ => (PropertyPath)$"child{Array.IndexOf(children, _)}", _ => _);

    static Dictionary<PropertyPath, ChildrenDefinition> Nested(params ChildrenDefinition[] nested) =>
        nested.ToDictionary(_ => (PropertyPath)$"nested{Array.IndexOf(nested, _)}", _ => _);
}
