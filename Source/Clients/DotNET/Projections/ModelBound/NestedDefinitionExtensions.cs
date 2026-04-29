// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing <see cref="NestedAttribute"/> and building nested definitions.
/// </summary>
static class NestedDefinitionExtensions
{
    /// <summary>
    /// Processes a <see cref="NestedAttribute"/> on a member and adds the nested definition to the projection definition.
    /// </summary>
    /// <param name="definition">The projection definition to add the nested entry to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="memberType">The type of the member (the nullable nested type).</param>
    /// <param name="processMember">The action to process child members recursively.</param>
    /// <param name="parentModelType">The type of the parent model that contains this nested property.</param>
    internal static void ProcessNestedAttribute(
        this ProjectionDefinition definition,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        Type? parentModelType = null)
    {
        var nestedType = GetNestedType(memberType);
        if (nestedType is null)
        {
            return;
        }

        var propertyPath = new PropertyPath(memberName);
        var propertyName = namingPolicy.GetPropertyName(propertyPath);

        if (!definition.Nested.TryGetValue(propertyName, out var nestedDef))
        {
            var shouldAutoMap = !Attribute.IsDefined(nestedType, typeof(NoAutoMapAttribute), inherit: true) &&
                               (parentModelType is null || !Attribute.IsDefined(parentModelType, typeof(NoAutoMapAttribute), inherit: true));

            nestedDef = new ChildrenDefinition
            {
                IdentifiedBy = PropertyPath.NotSet,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                Children = new Dictionary<string, ChildrenDefinition>(),
                Nested = new Dictionary<string, ChildrenDefinition>(),
                All = new FromEveryDefinition(),
                RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
                RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
                AutoMap = shouldAutoMap ? (Contracts.Projections.AutoMap)AutoMap.Enabled : (Contracts.Projections.AutoMap)AutoMap.Disabled
            };

            definition.Nested[propertyName] = nestedDef;
        }

        ProcessNestedTypeDefinition(nestedType, nestedDef, getOrCreateEventType, namingPolicy, processMember, definition, parentModelType);
    }

    /// <summary>
    /// Processes a <see cref="NestedAttribute"/> on a member within a parent children definition.
    /// </summary>
    /// <param name="parentChildrenDef">The parent children definition to add the nested entry to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="memberType">The type of the member (the nullable nested type).</param>
    /// <param name="processMember">The action to process child members recursively.</param>
    /// <param name="definition">The root projection definition.</param>
    /// <param name="parentModelType">The type of the parent model that contains this nested property.</param>
    internal static void ProcessNestedAttributeForChildren(
        this ChildrenDefinition parentChildrenDef,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        ProjectionDefinition definition,
        Type? parentModelType = null)
    {
        var nestedType = GetNestedType(memberType);
        if (nestedType is null)
        {
            return;
        }

        var propertyPath = new PropertyPath(memberName);
        var propertyName = namingPolicy.GetPropertyName(propertyPath);

        if (!parentChildrenDef.Nested.TryGetValue(propertyName, out var nestedDef))
        {
            var shouldAutoMap = !Attribute.IsDefined(nestedType, typeof(NoAutoMapAttribute), inherit: true) &&
                               (parentModelType is null || !Attribute.IsDefined(parentModelType, typeof(NoAutoMapAttribute), inherit: true));

            nestedDef = new ChildrenDefinition
            {
                IdentifiedBy = PropertyPath.NotSet,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                Children = new Dictionary<string, ChildrenDefinition>(),
                Nested = new Dictionary<string, ChildrenDefinition>(),
                All = new FromEveryDefinition(),
                RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
                RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
                AutoMap = shouldAutoMap ? (Contracts.Projections.AutoMap)AutoMap.Enabled : (Contracts.Projections.AutoMap)AutoMap.Disabled
            };

            parentChildrenDef.Nested[propertyName] = nestedDef;
        }

        ProcessNestedTypeDefinition(nestedType, nestedDef, getOrCreateEventType, namingPolicy, processMember, definition, parentModelType);
    }

    static void ProcessNestedTypeDefinition(
        Type nestedType,
        ChildrenDefinition nestedDef,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        ProjectionDefinition definition,
        Type? parentModelType)
    {
        // Process class-level ClearWith attributes on the nested type
        ProcessClearWithAttributes(nestedType, nestedDef, getOrCreateEventType);

        // Collect class-level FromEvent attributes on the nested type
        var classLevelFromEvents = nestedType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
            .ToList();

        // Initialize From definitions for class-level FromEvent attributes
        foreach (var attr in classLevelFromEvents)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventTypeId = getOrCreateEventType(eventType);
            if (!nestedDef.From.ContainsKey(eventTypeId))
            {
                nestedDef.From[eventTypeId] = new FromDefinition
                {
                    Key = WellKnownExpressions.EventSourceId,
                    Properties = new Dictionary<string, string>()
                };
            }
        }

        // Process constructor parameters for [Nested] attributes on record types
        var primaryConstructor = nestedType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        var constructorParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.GetParameters())
            {
                constructorParamNames.Add(parameter.Name!);

                if (parameter.IsDefined(typeof(NestedAttribute), inherit: false))
                {
                    nestedDef.ProcessNestedAttributeForChildren(
                        getOrCreateEventType,
                        namingPolicy,
                        parameter.Name!,
                        parameter.ParameterType,
                        processMember,
                        definition,
                        nestedType);
                }
            }
        }

        // Process properties of the nested type and detect any further [Nested] properties within them
        foreach (var childProperty in nestedType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            processMember(childProperty, definition, classLevelFromEvents, false, parentModelType, nestedDef);

            // Check property-level [Nested] only if not already handled as a constructor parameter
            if (!constructorParamNames.Contains(childProperty.Name) &&
                Attribute.IsDefined(childProperty, typeof(NestedAttribute)))
            {
                nestedDef.ProcessNestedAttributeForChildren(
                    getOrCreateEventType,
                    namingPolicy,
                    childProperty.Name,
                    childProperty.PropertyType,
                    processMember,
                    definition,
                    nestedType);
            }
        }
    }

    static void ProcessClearWithAttributes(
        Type nestedType,
        ChildrenDefinition nestedDef,
        Func<Type, EventType> getOrCreateEventType)
    {
        foreach (var attr in nestedType.GetCustomAttributes())
        {
            if (attr.GetType().IsGenericType &&
                attr.GetType().GetGenericTypeDefinition() == typeof(ClearWithAttribute<>))
            {
                var eventType = attr.GetType().GetGenericArguments()[0];
                var eventTypeId = getOrCreateEventType(eventType);
                nestedDef.RemovedWith[eventTypeId] = new RemovedWithDefinition
                {
                    Key = WellKnownExpressions.EventSourceId
                };
            }
        }
    }

    static Type? GetNestedType(Type propertyType)
    {
        // Handle nullable value types and reference types (T? or T)
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return propertyType.GetGenericArguments()[0];
        }

        // For reference types, return as-is (they're always nullable with nullable reference types)
        if (propertyType.IsClass || propertyType.IsInterface)
        {
            return propertyType;
        }

        return null;
    }
}
