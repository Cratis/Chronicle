// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition for a collection of property actions to perform for all events in the projection.
/// </summary>
/// <param name="Properties">Properties and expressions for each property.</param>
/// <param name="IncludeChildren">Include event types from child projections.</param>
public record FromEveryDefinition(IDictionary<PropertyPath, string> Properties, bool IncludeChildren)
{
    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public AutoMap AutoMap { get; set; } = AutoMap.Inherit;

    /// <summary>
    /// Gets or sets the key the projection folds every event into.
    /// </summary>
    /// <remarks>
    /// An event type a projection subscribes to explicitly carries its key on its own <see cref="FromDefinition"/>,
    /// but one reached by subscribing to every event has no such definition to carry it - the whole point being
    /// that the event type need not have existed when the projection was declared. Without a key here the only
    /// answer available is the event source id, which ties such a projection to counting per stream and makes
    /// aggregating by anything else - an event type, a namespace, a tenant - impossible to express.
    /// <para>
    /// Defaults to the event source id, so a definition written before this existed, or by a client that does not
    /// know about it, keys exactly as it did.
    /// </para>
    /// </remarks>
    public PropertyExpression Key { get; set; } = WellKnownExpressions.EventSourceId;
}
