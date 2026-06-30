// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

#pragma warning disable SA1313

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
        var childrenDef = ProcessChildrenFromAttributeCore(
            definition.Children,
            getOrCreateEventType,
            namingPolicy,
            memberName,
            memberType,
            attr,
            eventType,
            processMember,
            definition,
            parentModelType,
            includeSelfReferencingEvents: false);

        var visited = parentModelType is null ? new HashSet<Type>() : new HashSet<Type> { parentModelType };

        // Recursively process nested children on the child type
        var childType = GetChildType(memberType);
        if (childType is not null)
        {
            ProcessNestedChildren(childType, getOrCreateEventType, namingPolicy, processMember, definition, childrenDef, visited);
        }
    }

    /// <summary>
    /// Processes a ChildrenFrom attribute and adds the children definition to a parent children definition (for nested children).
    /// </summary>
    /// <param name="parentChildrenDef">The parent children definition to add the nested children to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <param name="attr">The ChildrenFrom attribute to process.</param>
    /// <param name="eventType">The event type to create children from.</param>
    /// <param name="processMember">The action to process child members recursively.</param>
    /// <param name="definition">The root projection definition for processing member attributes.</param>
    /// <param name="parentModelType">The type of the parent model that contains this children collection.</param>
    /// <param name="visitedChildTypes">Set of child types already visited in the current recursion chain, used to break cycles for self-referential or mutually recursive types.</param>
    internal static void ProcessChildrenFromAttributeForNestedChildren(
        this ChildrenDefinition parentChildrenDef,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Attribute attr,
        Type eventType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        ProjectionDefinition definition,
        Type? parentModelType = null,
        HashSet<Type>? visitedChildTypes = null)
    {
        var childType = GetChildType(memberType);
        var includeSelfReferencingEvents = childType is not null && visitedChildTypes?.Contains(childType) == true;

        // Events that create children of an ancestor collection (everything in the visited chain) must not be
        // propagated into this self-referential collection — only the current collection's own creator and
        // genuine update events belong here.
        var ancestorCreatorEventTypes = (visitedChildTypes ?? [])
            .SelectMany(GetChildrenFromEventTypes)
            .Where(type => type != eventType)
            .ToHashSet();

        var childrenDef = ProcessChildrenFromAttributeCore(
            parentChildrenDef.Children,
            getOrCreateEventType,
            namingPolicy,
            memberName,
            memberType,
            attr,
            eventType,
            processMember,
            definition,
            parentModelType,
            includeSelfReferencingEvents,
            ancestorCreatorEventTypes);

        // Recursively process nested children on the child type
        if (childType is not null)
        {
            ProcessNestedChildren(childType, getOrCreateEventType, namingPolicy, processMember, definition, childrenDef, visitedChildTypes ?? []);
        }
    }

    static ChildrenDefinition ProcessChildrenFromAttributeCore(
        IDictionary<string, ChildrenDefinition> targetChildren,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        string memberName,
        Type memberType,
        Attribute attr,
        Type eventType,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        ProjectionDefinition definition,
        Type? parentModelType = null,
        bool includeSelfReferencingEvents = false,
        IReadOnlySet<Type>? ancestorCreatorEventTypes = null)
    {
        var propertyPath = new PropertyPath(memberName);
        var propertyName = namingPolicy.GetPropertyName(propertyPath);
        var eventTypeId = getOrCreateEventType(eventType);

        var keyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.Key));
        var identifiedByProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.IdentifiedBy));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var explicitParentKey = parentKeyProperty?.GetValue(attr) as string;
        var discoveredParentKey = explicitParentKey is null ? DiscoverEventPropertyForParentId(eventType, parentModelType, key, namingPolicy) : null;
        var parentKey = explicitParentKey ?? discoveredParentKey ?? WellKnownExpressions.EventSourceId;

        var childType = GetChildType(memberType);
        var identifiedBy = identifiedByProperty?.GetValue(attr) as string;
        if (string.IsNullOrEmpty(identifiedBy))
        {
            identifiedBy = DiscoverKeyPropertyName(childType);
        }

        // Check if child type or parent type has NoAutoMapAttribute to determine if auto-mapping should be disabled
        // If parent has NoAutoMap, children should also not auto-map (inheritance of the policy)
        var shouldAutoMap = childType?.GetCustomAttributes(typeof(NoAutoMapAttribute), inherit: true).Length == 0 &&
                           (parentModelType?.GetCustomAttributes(typeof(NoAutoMapAttribute), inherit: true).Length == 0);

        // Apply naming policy to identifiedBy to ensure consistent casing
        var identifiedByWithNaming = string.IsNullOrEmpty(identifiedBy) || identifiedBy == WellKnownExpressions.EventSourceId
            ? identifiedBy
            : namingPolicy.GetPropertyName(new PropertyPath(identifiedBy));

        if (!targetChildren.TryGetValue(propertyName, out var childrenDef))
        {
            childrenDef = new ChildrenDefinition
            {
                IdentifiedBy = identifiedByWithNaming,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                Children = new Dictionary<string, ChildrenDefinition>(),
                All = new FromEveryDefinition(),
                RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
                RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
                AutoMap = shouldAutoMap ? (Contracts.Projections.AutoMap)AutoMap.Enabled : (Contracts.Projections.AutoMap)AutoMap.Disabled
            };
            targetChildren[propertyName] = childrenDef;
        }

        var keyExpression = key == WellKnownExpressions.EventSourceId ? key : namingPolicy.GetPropertyName(new PropertyPath(key));
        var parentKeyExpression = parentKey == WellKnownExpressions.EventSourceId ? parentKey : namingPolicy.GetPropertyName(new PropertyPath(parentKey));

        childrenDef.From[eventTypeId] = new FromDefinition
        {
            Key = keyExpression,
            ParentKey = parentKeyExpression,
            Properties = new Dictionary<string, string>()
        };

        if (childType is not null)
        {
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
                        fromDefinition.Properties[paramPropertyName] = $"{WellKnownExpressions.EventContext}({propertyToUse})";
                    }

                    // Process SetValue attributes on constructor parameters
                    foreach (var (setValueAttr, setValueEventType) in parameter.GetAttributesOfGenericType<SetValueAttribute<object>>())
                    {
                        var valueProperty = setValueAttr.GetType().GetProperty(nameof(SetValueAttribute<object>.Value));
                        var value = valueProperty?.GetValue(setValueAttr);
                        if (value is not null)
                        {
                            var invariantValue = ConvertValueToInvariantString(value);
                            childrenDef.From.AddSetValueMapping(getOrCreateEventType, setValueEventType, paramPropertyName, invariantValue);
                        }
                    }

                    // Process SetFrom attributes on constructor parameters
                    foreach (var (setFromAttr, setFromEventType) in parameter.GetAttributesOfGenericType<SetFromAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(setFromEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        var eventPropertyNameProperty = setFromAttr.GetType().GetProperty(nameof(SetFromAttribute<object>.EventPropertyName));
                        var eventPropertyName = eventPropertyNameProperty?.GetValue(setFromAttr) as string;
                        var propertyToUse = eventPropertyName ?? parameter.Name!;
                        childrenDef.From.AddSetMapping(getOrCreateEventType, namingPolicy, setFromEventType, paramPropertyName, propertyToUse);
                    }

                    // Process AddFrom attributes on constructor parameters
                    foreach (var (addFromAttr, addFromEventType) in parameter.GetAttributesOfGenericType<AddFromAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(addFromEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        var eventPropertyNameProperty = addFromAttr.GetType().GetProperty(nameof(AddFromAttribute<object>.EventPropertyName));
                        var eventPropertyName = eventPropertyNameProperty?.GetValue(addFromAttr) as string;
                        var propertyToUse = eventPropertyName ?? parameter.Name!;
                        childrenDef.From.AddAddMapping(getOrCreateEventType, namingPolicy, addFromEventType, paramPropertyName, propertyToUse);
                    }

                    // Process SubtractFrom attributes on constructor parameters
                    foreach (var (subtractFromAttr, subtractFromEventType) in parameter.GetAttributesOfGenericType<SubtractFromAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(subtractFromEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        var eventPropertyNameProperty = subtractFromAttr.GetType().GetProperty(nameof(SubtractFromAttribute<object>.EventPropertyName));
                        var eventPropertyName = eventPropertyNameProperty?.GetValue(subtractFromAttr) as string;
                        var propertyToUse = eventPropertyName ?? parameter.Name!;
                        childrenDef.From.AddSubtractMapping(getOrCreateEventType, namingPolicy, subtractFromEventType, paramPropertyName, propertyToUse);
                    }

                    // Process Increment attributes on constructor parameters
                    foreach (var (incrementAttr, incrementEventType) in parameter.GetAttributesOfGenericType<IncrementAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(incrementEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        childrenDef.From.AddIncrementMapping(getOrCreateEventType, incrementEventType, paramPropertyName);
                    }

                    // Process Decrement attributes on constructor parameters
                    foreach (var (decrementAttr, decrementEventType) in parameter.GetAttributesOfGenericType<DecrementAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(decrementEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        childrenDef.From.AddDecrementMapping(getOrCreateEventType, decrementEventType, paramPropertyName);
                    }

                    // Process Count attributes on constructor parameters
                    foreach (var (countAttr, countEventType) in parameter.GetAttributesOfGenericType<CountAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(countEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        childrenDef.From.AddCountMapping(getOrCreateEventType, countEventType, paramPropertyName);
                    }

                    // Process SetValue attributes on constructor parameters
                    foreach (var (setValueAttr, setValueEventType) in parameter.GetAttributesOfGenericType<SetValueAttribute<object>>())
                    {
                        if (!ShouldPropagateChildMemberEvent(setValueEventType, childType, includeSelfReferencingEvents))
                        {
                            continue;
                        }

                        var valueProperty = setValueAttr.GetType().GetProperty(nameof(SetValueAttribute<object>.Value));
                        var value = valueProperty?.GetValue(setValueAttr);
                        if (value is not null)
                        {
                            var invariantValue = FromDefinitionExtensions.ConvertValueToInvariantString(value);
                            childrenDef.From.AddSetValueMapping(getOrCreateEventType, setValueEventType, paramPropertyName, invariantValue);
                        }
                    }

                    // Check if this parameter has any explicit mapping attributes
                    var hasExplicitMapping = parameter.GetCustomAttributes()
                        .Any(a => a.GetType().IsGenericType &&
                                   (a.GetType().GetGenericTypeDefinition() == typeof(SetFromContextAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(SetFromAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(AddFromAttribute<>) ||
                                    a.GetType().GetGenericTypeDefinition() == typeof(SubtractFromAttribute<>) ||
                                     a.GetType().GetGenericTypeDefinition() == typeof(SetValueAttribute<>)));

                    var isIdentifiedByProperty = parameter.Name!.Equals(identifiedBy, StringComparison.OrdinalIgnoreCase);
                    var hasKeyAttribute = parameter.GetCustomAttribute<KeyAttribute>() is not null;
                    var keyExpressionMatchesParameter = paramPropertyName.Equals(keyExpression, StringComparison.OrdinalIgnoreCase);

                    // If this is the identified property and has no explicit mapping, map it to the key
                    // BUT skip if the key expression matches the parameter name AND it doesn't have [Key] attribute
                    // - This prevents auto-mapping properties that are only identifiers, not actual fields needing values
                    if (shouldAutoMap && !hasExplicitMapping && isIdentifiedByProperty)
                    {
                        var shouldSkipMapping = keyExpressionMatchesParameter && !hasKeyAttribute;
                        if (!shouldSkipMapping)
                        {
                            // If key is EventSourceId, use event context, otherwise use the key property from the event
                            if (key == WellKnownExpressions.EventSourceId)
                            {
                                fromDefinition.Properties[paramPropertyName] = $"{WellKnownExpressions.EventContext}(EventSourceId)";
                            }
                            else
                            {
                                // Map to the key from the event (keyExpression already has naming policy applied)
                                fromDefinition.Properties[paramPropertyName] = keyExpression;
                            }
                        }
                    }

                    // Check if parameter has [Key] attribute and no explicit mapping and key is EventSourceId
                    if (shouldAutoMap && !hasExplicitMapping && key == WellKnownExpressions.EventSourceId && hasKeyAttribute && !isIdentifiedByProperty)
                    {
                        fromDefinition.Properties[paramPropertyName] = $"{WellKnownExpressions.EventContext}(EventSourceId)";
                    }

                    if (includeSelfReferencingEvents && shouldAutoMap && !hasExplicitMapping)
                    {
                        foreach (var (fromEventAttr, fromEventType) in childType
                            .GetAttributesOfGenericType<FromEventAttribute<object>>()
                            .Where(pair =>
                                ShouldPropagateChildClassLevelFromEventForSelfReference(pair.Attribute, eventType, ancestorCreatorEventTypes) &&
                                pair.EventType.GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance) is not null))
                        {
                            childrenDef.From.AddSetMapping(getOrCreateEventType, namingPolicy, fromEventType, paramPropertyName, parameter.Name!);
                        }
                    }

                    // Process Join attributes on constructor parameters
                    foreach (var (joinAttr, joinEventType) in parameter.GetAttributesOfGenericType<JoinAttribute<object>>())
                    {
                        childrenDef.Join.ProcessJoinAttribute(getOrCreateEventType, namingPolicy, joinAttr, joinEventType, parameter.Name!, paramPropertyName);
                    }

                    // Process RemovedWith attributes on constructor parameters
                    foreach (var (removedAttr, removedEventType) in parameter.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
                    {
                        childrenDef.RemovedWith.ProcessRemovedWithAttribute(getOrCreateEventType, namingPolicy, removedAttr, removedEventType);
                    }

                    // Process RemovedWithJoin attributes on constructor parameters
                    foreach (var (removedJoinAttr, removedJoinEventType) in parameter.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
                    {
                        childrenDef.RemovedWithJoin.ProcessRemovedWithJoinAttribute(getOrCreateEventType, namingPolicy, removedJoinAttr, removedJoinEventType);
                    }
                }
            }

            // Property auto-mapping is handled by the server based on the AutoMap flag
            // No need to explicitly map properties here

            // Process class-level RemovedWith attributes on the child type
            ProcessChildTypeLevelRemovedWith(childType, getOrCreateEventType, namingPolicy, childrenDef);

            // Collect class-level FromEvent attributes on the child type
            var childClassLevelFromEvents = childType.GetCustomAttributes()
                .Where(attr => attr.GetType().IsGenericType &&
                              attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>) &&
                              (includeSelfReferencingEvents
                                  ? ShouldPropagateChildClassLevelFromEventForSelfReference(attr, eventType, ancestorCreatorEventTypes)
                                  : ShouldPropagateChildClassLevelFromEvent(attr, childType)))
                .ToList();

            // Process properties for attributes (this handles SetFromContext and other attributes on properties)
            foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                processMember(childProperty, definition, childClassLevelFromEvents, false, childType, childrenDef);
            }
        }

        return childrenDef;
    }

    static void ProcessNestedChildren(
        Type childType,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember,
        ProjectionDefinition definition,
        ChildrenDefinition parentChildrenDef,
        HashSet<Type> visitedChildTypes)
    {
        if (!visitedChildTypes.Add(childType))
        {
            return;
        }

        // Process constructor parameters for ChildrenFrom and Nested attributes
        var primaryConstructor = childType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        var constructorParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.GetParameters())
            {
                constructorParamNames.Add(parameter.Name!);

                foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
                {
                    parentChildrenDef.ProcessChildrenFromAttributeForNestedChildren(
                        getOrCreateEventType,
                        namingPolicy,
                        parameter.Name!,
                        parameter.ParameterType,
                        attr,
                        eventType,
                        processMember,
                        definition,
                        childType,
                        visitedChildTypes);
                }

                if (parameter.IsDefined(typeof(NestedAttribute), inherit: false))
                {
                    parentChildrenDef.ProcessNestedAttributeForChildren(
                        getOrCreateEventType,
                        namingPolicy,
                        parameter.Name!,
                        parameter.ParameterType,
                        processMember,
                        definition,
                        childType);
                }
            }
        }

        // Process properties for ChildrenFrom and Nested attributes
        foreach (var property in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip if already processed as constructor parameter
            if (constructorParamNames.Contains(property.Name))
            {
                continue;
            }

            foreach (var (attr, eventType) in property.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
            {
                parentChildrenDef.ProcessChildrenFromAttributeForNestedChildren(
                    getOrCreateEventType,
                    namingPolicy,
                    property.Name,
                    property.PropertyType,
                    attr,
                    eventType,
                    processMember,
                    definition,
                    childType,
                    visitedChildTypes);
            }

            if (Attribute.IsDefined(property, typeof(NestedAttribute)))
            {
                parentChildrenDef.ProcessNestedAttributeForChildren(
                    getOrCreateEventType,
                    namingPolicy,
                    property.Name,
                    property.PropertyType,
                    processMember,
                    definition,
                    childType);
            }
        }
    }

    static void ProcessChildTypeLevelRemovedWith(
        Type childType,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        ChildrenDefinition childrenDef)
    {
        foreach (var (attr, eventType) in childType.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            childrenDef.RemovedWith.ProcessRemovedWithAttribute(getOrCreateEventType, namingPolicy, attr, eventType);
        }

        foreach (var (attr, eventType) in childType.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            childrenDef.RemovedWithJoin.ProcessRemovedWithJoinAttribute(getOrCreateEventType, namingPolicy, attr, eventType);
        }
    }

    static bool ShouldPropagateChildClassLevelFromEvent(Attribute fromEventAttribute, Type childType)
    {
        var hasExplicitKey = fromEventAttribute is IKeyedAttribute keyedAttribute &&
                             !string.IsNullOrEmpty(keyedAttribute.Key);
        if (hasExplicitKey)
        {
            return true;
        }

        var eventType = fromEventAttribute.GetType().GetGenericArguments()[0];
        return !HasChildrenFromForEventType(childType, eventType);
    }

    static bool ShouldPropagateChildClassLevelFromEventForSelfReference(
        Attribute fromEventAttribute,
        Type childCreatingEventType,
        IReadOnlySet<Type>? ancestorCreatorEventTypes)
    {
        var childLevelEventType = fromEventAttribute.GetType().GetGenericArguments()[0];

        // Always propagate if it's the child-creating event itself
        if (childLevelEventType == childCreatingEventType)
        {
            return true;
        }

        // Never propagate an event that creates children of an ANCESTOR collection (e.g. the event that adds
        // top-level features) into a descendant self-referential collection. Doing so would materialize the
        // ancestor's instances as (self-)children of the descendant.
        if (ancestorCreatorEventTypes?.Contains(childLevelEventType) == true)
        {
            return false;
        }

        // For other events: check if they are keyed update events
        var keyProperty = fromEventAttribute.GetType().GetProperty(nameof(FromEventAttribute<object>.Key));
        var parentKeyProperty = fromEventAttribute.GetType().GetProperty(nameof(FromEventAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(fromEventAttribute) as string;
        var parentKey = parentKeyProperty?.GetValue(fromEventAttribute) as string;

        // If it doesn't have an explicit key, it's not a keyed event, so propagate it
        if (string.IsNullOrEmpty(key))
        {
            return true;
        }

        // It has an explicit key. Only propagate if it also has an EXPLICIT parent key.
        // Keyed update events without explicit parent keys should not be propagated in self-referential cases,
        // as they would be upserted as self-children when resolved via EventSourceId fallback.
        return !string.IsNullOrEmpty(parentKey);
    }

    static bool ShouldPropagateChildMemberEvent(Type eventType, Type? childType, bool includeSelfReferencingEvents)
    {
        if (includeSelfReferencingEvents || childType is null)
        {
            return true;
        }

        return !HasChildrenFromForEventType(childType, eventType);
    }

    static bool HasChildrenFromForEventType(Type childType, Type eventType)
    {
        var primaryConstructor = childType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (primaryConstructor?.GetParameters().Any(parameter => parameter.GetCustomAttributes().Any(attr => IsChildrenFromAttributeForEventType(attr, eventType))) == true)
        {
            return true;
        }

        return childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.GetCustomAttributes().Any(attr => IsChildrenFromAttributeForEventType(attr, eventType)));
    }

    static bool IsChildrenFromAttributeForEventType(Attribute attribute, Type eventType) =>
        attribute.GetType().IsGenericType &&
        attribute.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>) &&
        attribute.GetType().GetGenericArguments()[0] == eventType;

    static IEnumerable<Type> GetChildrenFromEventTypes(Type type)
    {
        var primaryConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        var fromConstructor = primaryConstructor?.GetParameters()
            .SelectMany(parameter => parameter.GetAttributesOfGenericType<ChildrenFromAttribute<object>>().Select(pair => pair.EventType))
            ?? [];

        var fromProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(property => property.GetAttributesOfGenericType<ChildrenFromAttribute<object>>().Select(pair => pair.EventType));

        return fromConstructor.Concat(fromProperties);
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

        var keyProperty = properties.FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute), true));
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

    /// <summary>
    /// Infers the event property to use as the parent key for a <c>[ChildrenFrom]</c> collection when no
    /// explicit <c>parentKey</c> is supplied, by matching the parent read model's identifier type.
    /// </summary>
    /// <param name="eventType">The event type the children are built from.</param>
    /// <param name="parentModelType">The parent read model type, or <see langword="null"/> when unavailable.</param>
    /// <param name="childKey">The child key property name, excluded from the candidates.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> used to shape the resolved property name.</param>
    /// <returns>
    /// The inferred parent-key property name, or <see langword="null"/> when no candidate matches (the caller
    /// then falls back to <see cref="WellKnownExpressions.EventSourceId"/> for the same-event-source case).
    /// </returns>
    /// <remarks>
    /// The child <paramref name="childKey"/> property is excluded from the candidates — a property is the
    /// child's own key or the parent reference, never both. When more than one property still matches the
    /// parent identifier type the inference is ambiguous and the first by declaration order is used; supply
    /// an explicit <c>parentKey</c> to disambiguate.
    /// </remarks>
    static string? DiscoverEventPropertyForParentId(Type eventType, Type? parentModelType, string childKey, INamingPolicy namingPolicy)
    {
        if (parentModelType is null)
        {
            return null;
        }

        var parentIdType = GetIdentifierType(parentModelType);
        if (parentIdType is null)
        {
            return null;
        }

        var match = Array.Find(
            eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            p => p.PropertyType == parentIdType && !p.Name.Equals(childKey, StringComparison.OrdinalIgnoreCase));

        return match is not null
            ? namingPolicy.GetPropertyName(new PropertyPath(match.Name))
            : null;
    }

    static Type? GetIdentifierType(Type modelType)
    {
        var idProperty = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty is not null)
        {
            return idProperty.PropertyType;
        }

        var constructor = modelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        return constructor?.GetParameters()
            .FirstOrDefault(p => p.Name?.Equals("Id", StringComparison.OrdinalIgnoreCase) == true)
            ?.ParameterType;
    }

    static string ConvertValueToInvariantString(object value)
    {
        var actualValue = value;

        if (actualValue.GetType().IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(actualValue.GetType());
            if (underlyingType == typeof(int))
            {
                actualValue = Convert.ChangeType(actualValue, underlyingType);
            }
            else
            {
                return actualValue.ToString()!;
            }
        }

        return actualValue switch
        {
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("o", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => actualValue.ToString()!
        };
    }
}
