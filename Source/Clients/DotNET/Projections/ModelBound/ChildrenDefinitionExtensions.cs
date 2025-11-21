// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Keys;
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
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <param name="attr">The ChildrenFrom attribute to process.</param>
    /// <param name="eventType">The event type to create children from.</param>
    /// <param name="processMember">The action to process child members recursively.</param>
    /// <param name="parentModelType">The type of the parent model that contains this children collection.</param>
    internal static void ProcessChildrenFromAttribute(
        this ProjectionDefinition definition,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Attribute attr,
        Type eventType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        Type? parentModelType = null)
    {
        var propertyPath = new PropertyPath(memberName);
        var propertyName = namingPolicy.GetPropertyName(propertyPath);
        var eventTypeId = getOrCreateEventType(eventType);

        var keyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.Key));
        var identifiedByProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.IdentifiedBy));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.ParentKey));
        var autoMapProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.AutoMap));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var explicitParentKey = parentKeyProperty?.GetValue(attr) as string;
        var discoveredParentKey = explicitParentKey is null ? DiscoverEventPropertyForParentId(eventType, parentModelType, namingPolicy) : null;
        var parentKey = explicitParentKey ?? discoveredParentKey ?? WellKnownExpressions.EventSourceId;
        var autoMap = autoMapProperty?.GetValue(attr) as bool? ?? true;

        var childType = GetChildType(memberType);
        var identifiedBy = identifiedByProperty?.GetValue(attr) as string;
        if (string.IsNullOrEmpty(identifiedBy))
        {
            identifiedBy = DiscoverKeyPropertyName(childType);
        }

        if (!definition.Children.TryGetValue(propertyName, out var childrenDef))
        {
            childrenDef = new ChildrenDefinition
            {
                IdentifiedBy = identifiedBy,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                All = new FromEveryDefinition(),
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

        if (childType is not null)
        {
            // If autoMap is enabled, map matching properties from event to child model
            if (autoMap)
            {
                var fromDefinition = childrenDef.From[eventTypeId];
                fromDefinition.AutoMapMatchingProperties(namingPolicy, eventType, childType);
            }

            // For records, process constructor parameters to pick up attributes
            var primaryConstructor = childType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (primaryConstructor is not null)
            {
                var fromDefinition = childrenDef.From[eventTypeId];

                foreach (var parameter in primaryConstructor.GetParameters())
                {
                    var paramPath = new PropertyPath(parameter.Name!);
                    var paramPropertyName = namingPolicy.GetPropertyName(paramPath);

                    // Process SetFromContext attributes on constructor parameters
                    var setFromContextAttrs = parameter.GetCustomAttributes()
                        .Where(a => a.GetType().IsGenericType &&
                                     a.GetType().GetGenericTypeDefinition() == typeof(SetFromContextAttribute<>))
                        .ToList();

                    foreach (var contextAttr in setFromContextAttrs)
                    {
                        var contextPropertyNameProperty = contextAttr.GetType().GetProperty(nameof(SetFromContextAttribute<object>.ContextPropertyName));
                        var contextPropertyName = contextPropertyNameProperty?.GetValue(contextAttr) as string;
                        var propertyToUse = contextPropertyName ?? parameter.Name!;
                        fromDefinition.Properties[paramPropertyName] = $"$eventContext({propertyToUse})";
                    }

                    // Check if this parameter has any explicit mapping attributes
                    var hasExplicitMapping = parameter.GetCustomAttributes()
                        .Any(a => a.GetType().IsGenericType &&
                                   (a.GetType().GetGenericTypeDefinition() == typeof(SetFromContextAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(SetFromAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(AddFromAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(SubtractFromAttribute<>)));

                    // Only auto-map Id/Key to EventSourceId if autoMap is enabled and there's no explicit mapping
                    if (autoMap && !hasExplicitMapping)
                    {
                        // If this is the identified property and has no explicit mapping, default to EventSourceId
                        if (parameter.Name!.Equals(identifiedBy, StringComparison.OrdinalIgnoreCase))
                        {
                            fromDefinition.Properties[paramPropertyName] = "$eventContext(EventSourceId)";
                        }

                        // Check if parameter has [Key] attribute and no explicit mapping
                        if (parameter.GetCustomAttribute<KeyAttribute>() is not null)
                        {
                            fromDefinition.Properties[paramPropertyName] = "$eventContext(EventSourceId)";
                        }
                    }
                }
            }

            // Process properties for attributes (this handles SetFromContext and other attributes on properties)
            foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                processMember(childProperty, definition, [], false, null, childrenDef);
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

    static string DiscoverKeyPropertyName(Type? childType)
    {
        if (childType is null)
        {
            return WellKnownExpressions.EventSourceId;
        }

        var properties = childType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>(true) is not null);
        if (keyProperty is not null)
        {
            return keyProperty.Name;
        }

        // Check constructor parameters for record types
        var constructor = childType.GetConstructors().FirstOrDefault();
        if (constructor is not null)
        {
            var keyParameter = constructor.GetParameters().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>(true) is not null);
            if (keyParameter is not null)
            {
                // Convert parameter name to property name (capitalize first letter)
                return char.ToUpperInvariant(keyParameter.Name![0]) + keyParameter.Name.Substring(1);
            }
        }

        var idProperty = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty is not null)
        {
            return idProperty.Name;
        }

        return WellKnownExpressions.EventSourceId;
    }

    static string? DiscoverEventPropertyForParentId(Type eventType, Type? parentModelType, INamingPolicy namingPolicy)
    {
        if (parentModelType is null)
        {
            return null;
        }

        // First, find the parent model's Id property and its type
        var parentIdProperty = parentModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (parentIdProperty is null)
        {
            // Check constructor parameters for record types
            var constructor = parentModelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            var idParameter = constructor?.GetParameters()
                .FirstOrDefault(p => p.Name?.Equals("Id", StringComparison.OrdinalIgnoreCase) == true);

            if (idParameter is null)
            {
                return null;
            }

            // Get the type from constructor parameter
            var parentIdType = idParameter.ParameterType;

            // Search event for a property with matching type
            var matchingEventProperty = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.PropertyType == parentIdType);

            return matchingEventProperty is not null
                ? namingPolicy.GetPropertyName(new PropertyPath(matchingEventProperty.Name))
                : null;
        }

        // Search event for a property with matching type
        var eventProperty = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == parentIdProperty.PropertyType);

        return eventProperty is not null
            ? namingPolicy.GetPropertyName(new PropertyPath(eventProperty.Name))
            : null;
    }
}
