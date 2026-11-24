// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for specifications that start from a projection definition and change it, which a definition does not allow itself.
/// Every method returns a changed copy and leaves the definition it was called on as it was.
/// </summary>
public static class ProjectionDefinitionEditing
{
    /// <summary>
    /// Gets a copy with a different from-definition for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="from">The <see cref="FromDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithFrom(this ProjectionDefinition definition, EventType eventType, FromDefinition from) =>
        definition with { From = Put(definition.From, eventType, from) };

    /// <inheritdoc cref="WithFrom(ProjectionDefinition, EventType, FromDefinition)"/>
    public static ChildrenDefinition WithFrom(this ChildrenDefinition definition, EventType eventType, FromDefinition from) =>
        definition with { From = Put(definition.From, eventType, from) };

    /// <summary>
    /// Gets a copy without any from-definitions.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithoutFrom(this ProjectionDefinition definition) =>
        definition with { From = new Dictionary<EventType, FromDefinition>() };

    /// <inheritdoc cref="WithoutFrom(ProjectionDefinition)"/>
    public static ChildrenDefinition WithoutFrom(this ChildrenDefinition definition) =>
        definition with { From = new Dictionary<EventType, FromDefinition>() };

    /// <summary>
    /// Gets a copy without the from-definition for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithoutFrom(this ProjectionDefinition definition, EventType eventType) =>
        definition with { From = definition.From.Where(_ => _.Key != eventType).ToDictionary() };

    /// <summary>
    /// Gets a copy with a different from-definition property mapping for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="property">The property to map.</param>
    /// <param name="expression">The expression it maps from.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithFromProperty(this ProjectionDefinition definition, EventType eventType, PropertyPath property, string expression) =>
        definition.WithFrom(eventType, definition.From[eventType] with { Properties = Put(definition.From[eventType].Properties, property, expression) });

    /// <summary>
    /// Gets a copy with a different join-definition for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="join">The <see cref="JoinDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithJoin(this ProjectionDefinition definition, EventType eventType, JoinDefinition join) =>
        definition with { Join = Put(definition.Join, eventType, join) };

    /// <inheritdoc cref="WithJoin(ProjectionDefinition, EventType, JoinDefinition)"/>
    public static ChildrenDefinition WithJoin(this ChildrenDefinition definition, EventType eventType, JoinDefinition join) =>
        definition with { Join = Put(definition.Join, eventType, join) };

    /// <summary>
    /// Gets a copy with a different removed-with-definition for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="removedWith">The <see cref="RemovedWithDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithRemovedWith(this ProjectionDefinition definition, EventType eventType, RemovedWithDefinition removedWith) =>
        definition with { RemovedWith = Put(definition.RemovedWith, eventType, removedWith) };

    /// <summary>
    /// Gets a copy with a different removed-with-join-definition for an event type.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="removedWithJoin">The <see cref="RemovedWithJoinDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithRemovedWithJoin(this ProjectionDefinition definition, EventType eventType, RemovedWithJoinDefinition removedWithJoin) =>
        definition with { RemovedWithJoin = Put(definition.RemovedWithJoin, eventType, removedWithJoin) };

    /// <inheritdoc cref="WithRemovedWithJoin(ProjectionDefinition, EventType, RemovedWithJoinDefinition)"/>
    public static ChildrenDefinition WithRemovedWithJoin(this ChildrenDefinition definition, EventType eventType, RemovedWithJoinDefinition removedWithJoin) =>
        definition with { RemovedWithJoin = Put(definition.RemovedWithJoin, eventType, removedWithJoin) };

    /// <summary>
    /// Gets a copy with a different child.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property the child is for.</param>
    /// <param name="child">The <see cref="ChildrenDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithChild(this ProjectionDefinition definition, PropertyPath property, ChildrenDefinition child) =>
        definition with { Children = Put(definition.Children, property, child) };

    /// <inheritdoc cref="WithChild(ProjectionDefinition, PropertyPath, ChildrenDefinition)"/>
    public static ChildrenDefinition WithChild(this ChildrenDefinition definition, PropertyPath property, ChildrenDefinition child) =>
        definition with { Children = Put(definition.Children, property, child) };

    /// <summary>
    /// Gets a copy with a child changed by a function.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property the child is for.</param>
    /// <param name="change">Function that gets the changed child.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithChild(this ProjectionDefinition definition, PropertyPath property, Func<ChildrenDefinition, ChildrenDefinition> change) =>
        definition.WithChild(property, change(definition.Children[property]));

    /// <inheritdoc cref="WithChild(ProjectionDefinition, PropertyPath, Func{ChildrenDefinition, ChildrenDefinition})"/>
    public static ChildrenDefinition WithChild(this ChildrenDefinition definition, PropertyPath property, Func<ChildrenDefinition, ChildrenDefinition> change) =>
        definition.WithChild(property, change(definition.Children[property]));

    /// <summary>
    /// Gets a copy without any children.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithoutChildren(this ProjectionDefinition definition) =>
        definition with { Children = new Dictionary<PropertyPath, ChildrenDefinition>() };

    /// <summary>
    /// Gets a copy with a different nested child.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property the nested child is for.</param>
    /// <param name="child">The <see cref="ChildrenDefinition"/>.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithNested(this ProjectionDefinition definition, PropertyPath property, ChildrenDefinition child) =>
        definition with { Nested = Put(definition.Nested ?? new Dictionary<PropertyPath, ChildrenDefinition>(), property, child) };

    /// <summary>
    /// Gets a copy with a different property mapping for every event.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property to map.</param>
    /// <param name="expression">The expression it maps from.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithEveryProperty(this ProjectionDefinition definition, PropertyPath property, string expression) =>
        definition with { FromEvery = definition.FromEvery with { Properties = Put(definition.FromEvery.Properties, property, expression) } };

    /// <inheritdoc cref="WithEveryProperty(ProjectionDefinition, PropertyPath, string)"/>
    public static ChildrenDefinition WithEveryProperty(this ChildrenDefinition definition, PropertyPath property, string expression) =>
        definition with { FromEvery = definition.FromEvery with { Properties = Put(definition.FromEvery.Properties, property, expression) } };

    /// <summary>
    /// Gets a copy without a property mapping for every event.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property that is mapped.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithoutEveryProperty(this ProjectionDefinition definition, PropertyPath property) =>
        definition with { FromEvery = definition.FromEvery with { Properties = definition.FromEvery.Properties.Where(_ => _.Key != property).ToDictionary() } };

    /// <summary>
    /// Gets a copy of a child with a different property mapping for every event, in both of the views the child has of its every-event rules.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The property to map.</param>
    /// <param name="expression">The expression it maps from.</param>
    /// <returns>The changed copy.</returns>
    public static ChildrenDefinition WithAllProperty(this ChildrenDefinition definition, PropertyPath property, string expression)
    {
        var all = definition.All with { Properties = Put(definition.All.Properties, property, expression) };
        return definition with { All = all, FromEvery = all };
    }

    /// <summary>
    /// Gets a copy with a different value in the initial model state.
    /// </summary>
    /// <param name="definition">The definition to copy.</param>
    /// <param name="property">The name of the property.</param>
    /// <param name="value">The value.</param>
    /// <returns>The changed copy.</returns>
    public static ProjectionDefinition WithInitialModelState(this ProjectionDefinition definition, string property, JsonNode? value)
    {
        var state = definition.InitialModelState.ToJsonObject();
        state[property] = value;
        return definition with { InitialModelState = state };
    }

    static Dictionary<TKey, TValue> Put<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, TKey key, TValue value)
        where TKey : notnull
    {
        return new Dictionary<TKey, TValue>(source) { [key] = value };
    }
}
