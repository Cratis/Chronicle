// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing ChildrenFrom attributes.
/// </summary>
static class ChildrenDefinitionExtensions
{
    /// <summary>
    /// Processes a ChildrenFrom attribute and adds the children definition to the projection definition.
    /// </summary>
    /// <param name="definition">The projection definition to add the children to.</param>
    /// <param name="eventTypes">The event types registry for resolving event type information.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <param name="attr">The ChildrenFrom attribute to process.</param>
    /// <param name="eventType">The event type to create children from.</param>
    /// <param name="processMember">The action to process child members recursively.</param>
    internal static void ProcessChildrenFromAttribute(
        this ProjectionDefinition definition,
        IEventTypes eventTypes,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Attribute attr,
        Type eventType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, ChildrenDefinition?> processMember)
    {
        var propertyPath = new PropertyPath(memberName);
        var propertyName = namingPolicy.GetPropertyName(propertyPath);
        var eventTypeId = eventTypes.GetEventTypeFor(eventType).ToContract();

        var keyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.Key));
        var identifiedByProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.IdentifiedBy));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var identifiedBy = identifiedByProperty?.GetValue(attr) as string ?? WellKnownExpressions.Id;
        var parentKey = parentKeyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        if (!definition.Children.TryGetValue(propertyName, out var childrenDef))
        {
            childrenDef = new ChildrenDefinition
            {
                IdentifiedBy = identifiedBy,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
                RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>()
            };
            definition.Children[propertyName] = childrenDef;
        }

        var keyExpression = key == WellKnownExpressions.EventSourceId ? key : namingPolicy.GetPropertyName(new PropertyPath(key));

        childrenDef.From[eventTypeId] = new FromDefinition
        {
            Key = keyExpression,
            ParentKey = parentKey,
            Properties = new Dictionary<string, string>()
        };

        var childType = GetChildType(memberType);
        if (childType is not null)
        {
            foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                processMember(childProperty, definition, [], false, childrenDef);
            }
        }
    }

    static Type? GetChildType(Type propertyType)
    {
        if (propertyType.IsGenericType)
        {
            var genericDef = propertyType.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) || genericDef.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return propertyType.GetGenericArguments()[0];
            }
        }

        var enumerableInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }
}
