// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing Join attributes.
/// </summary>
static class JoinDefinitionExtensions
{
    /// <summary>
    /// Processes a Join attribute and adds the join definition to the target dictionary.
    /// </summary>
    /// <param name="targetJoin">The target Join dictionary to add the definition to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="attr">The Join attribute to process.</param>
    /// <param name="eventType">The event type to join from.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="childIdentity">The serialized identifying property of the enclosing child collection, if any.</param>
    /// <exception cref="MissingChildIdentityForJoin">The join omits its target and the child has no identifying property.</exception>
    internal static void ProcessJoinAttribute(
        this IDictionary<EventType, JoinDefinition> targetJoin,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Attribute attr,
        Type eventType,
        string memberName,
        string propertyName,
        string? childIdentity = null)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var onProperty = attr.GetType().GetProperty(nameof(JoinAttribute<object>.On));
        var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(ICanMapToEventProperty.EventPropertyName));

        var on = onProperty?.GetValue(attr) as string;
        var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

        // Scalar [Nested] objects also use a children definition, but have no collection identity (NotSet).
        // They keep the property-name fallback, including nested objects inside child collections.
        if (childIdentity == PropertyPath.NotSetValue)
        {
            childIdentity = null;
        }

        if (!targetJoin.TryGetValue(eventTypeId, out var joinDef))
        {
            if (on is null && childIdentity == WellKnownExpressions.EventSourceId)
            {
                throw new MissingChildIdentityForJoin(eventType, memberName);
            }

            // Explicit targets carry CLR casing; the child identity and fallback property are already serialized.
            var normalizedOn = on is not null ? (string)ProjectionPropertyPaths.Resolve(on, namingPolicy, ProjectionPathRole.Target) : childIdentity ?? propertyName;
            joinDef = new JoinDefinition
            {
                On = normalizedOn,
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetJoin[eventTypeId] = joinDef;
        }

        var eventPropertyToUse = eventPropertyName ?? memberName;
        PropertyValidator.ValidatePropertyExists(eventType, eventPropertyToUse);
        joinDef.Properties[propertyName] = ProjectionPropertyPaths.Resolve(eventPropertyToUse, namingPolicy, ProjectionPathRole.Source);
    }
}
