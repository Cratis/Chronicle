// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
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
        var context = new ChildrenProcessingContext(getOrCreateEventType, namingPolicy, processMember, definition);
        var declaration = new ChildrenFromDeclaration(memberName, memberType, attr, eventType, parentModelType);
        var childrenDef = ProcessChildrenFromAttributeCore(definition.Children, context, declaration, ChildPropagation.None);

        var visited = parentModelType is null ? new HashSet<Type>() : new HashSet<Type> { parentModelType };

        // Recursively process nested children on the child type
        var childType = ChildTypeInspection.GetChildType(memberType);
        if (childType is not null)
        {
            NestedChildrenWalker.Walk(childType, context, childrenDef, visited);
        }
    }

    /// <summary>
    /// Processes a ChildrenFrom attribute and adds the children definition to a parent children definition (for nested children).
    /// </summary>
    /// <param name="parentChildrenDef">The parent children definition to add the nested children to.</param>
    /// <param name="context">The shared processing collaborators.</param>
    /// <param name="declaration">The ChildrenFrom declaration to process.</param>
    /// <param name="visitedChildTypes">Set of child types already visited in the current recursion chain, used to break cycles for self-referential or mutually recursive types.</param>
    internal static void ProcessChildrenFromAttributeForNestedChildren(
        this ChildrenDefinition parentChildrenDef,
        ChildrenProcessingContext context,
        ChildrenFromDeclaration declaration,
        HashSet<Type> visitedChildTypes)
    {
        var childType = ChildTypeInspection.GetChildType(declaration.MemberType);
        var includeSelfReferencingEvents = childType is not null && visitedChildTypes.Contains(childType);

        // Events that create children of an ancestor collection (everything in the visited chain) must not be
        // propagated into this self-referential collection — only the current collection's own creator and
        // genuine update events belong here.
        var ancestorCreatorEventTypes = visitedChildTypes
            .SelectMany(ChildTypeInspection.GetChildrenFromEventTypes)
            .Where(type => type != declaration.EventType)
            .ToHashSet();

        var childrenDef = ProcessChildrenFromAttributeCore(
            parentChildrenDef.Children,
            context,
            declaration,
            new ChildPropagation(includeSelfReferencingEvents, ancestorCreatorEventTypes));

        // Recursively process nested children on the child type
        if (childType is not null)
        {
            NestedChildrenWalker.Walk(childType, context, childrenDef, visitedChildTypes);
        }
    }

    static ChildrenDefinition ProcessChildrenFromAttributeCore(
        IDictionary<string, ChildrenDefinition> targetChildren,
        ChildrenProcessingContext context,
        ChildrenFromDeclaration declaration,
        ChildPropagation propagation)
    {
        var namingPolicy = context.NamingPolicy;
        var attr = declaration.Attribute;
        var propertyName = (string)ProjectionPropertyPaths.Resolve(declaration.MemberName, namingPolicy, ProjectionPathRole.Target);
        var eventTypeId = context.GetOrCreateEventType(declaration.EventType);

        var key = attr.ReadString(nameof(ChildrenFromAttribute<object>.Key)) ?? WellKnownExpressions.EventSourceId;
        var parentKey = attr.ReadString(nameof(ChildrenFromAttribute<object>.ParentKey))
            ?? ChildTypeInspection.DiscoverEventPropertyForParentId(declaration.EventType, declaration.ParentModelType, key, namingPolicy)
            ?? WellKnownExpressions.EventSourceId;

        var childType = ChildTypeInspection.ResolveConcreteChildType(ChildTypeInspection.GetChildType(declaration.MemberType));
        var identifiedBy = attr.ReadString(nameof(ChildrenFromAttribute<object>.IdentifiedBy));
        if (string.IsNullOrEmpty(identifiedBy))
        {
            identifiedBy = ChildTypeInspection.DiscoverKeyPropertyName(childType, key);
        }

        // If parent has NoAutoMap, children should also not auto-map (inheritance of the policy)
        var shouldAutoMap = HasNoNoAutoMapAttribute(childType) && HasNoNoAutoMapAttribute(declaration.ParentModelType);

        var childrenDef = GetOrCreateChildrenDefinition(targetChildren, propertyName, ApplyNamingToIdentifier(identifiedBy, namingPolicy), shouldAutoMap, childType, namingPolicy);

        ChildrenOrdering.Apply(childrenDef, ChildrenOrdering.Declared(attr, childType, declaration.MemberName, namingPolicy), declaration.MemberName);

        var identity = new ChildIdentity(key, ToSourceExpression(key, namingPolicy), identifiedBy);
        var fromDefinition = new FromDefinition
        {
            Key = identity.KeyExpression,
            ParentKey = ToSourceExpression(parentKey, namingPolicy),
            Properties = new Dictionary<string, string>()
        };
        childrenDef.From[eventTypeId] = fromDefinition;

        if (childType is not null)
        {
            var scope = new ChildMappingScope(context, childrenDef, fromDefinition, childType, declaration.EventType, identity, propagation, shouldAutoMap);
            MapChildType(scope);
        }

        return childrenDef;
    }

    static bool HasNoNoAutoMapAttribute(Type? type) =>
        type?.GetCustomAttributes(typeof(NoAutoMapAttribute), inherit: true).Length == 0;

    static string ApplyNamingToIdentifier(string identifiedBy, INamingPolicy namingPolicy) =>
        string.IsNullOrEmpty(identifiedBy) || identifiedBy == WellKnownExpressions.EventSourceId
            ? identifiedBy
            : (string)ProjectionPropertyPaths.Resolve(identifiedBy, namingPolicy, ProjectionPathRole.Target);

    static string ToSourceExpression(string key, INamingPolicy namingPolicy) =>
        key == WellKnownExpressions.EventSourceId ? key : (string)ProjectionPropertyPaths.Resolve(key, namingPolicy, ProjectionPathRole.Source);

    static ChildrenDefinition GetOrCreateChildrenDefinition(
        IDictionary<string, ChildrenDefinition> targetChildren,
        string propertyName,
        string identifiedBy,
        bool shouldAutoMap,
        Type? childType,
        INamingPolicy namingPolicy)
    {
        if (targetChildren.TryGetValue(propertyName, out var existing))
        {
            return existing;
        }

        var childrenDef = new ChildrenDefinition
        {
            IdentifiedBy = identifiedBy,
            From = new Dictionary<EventType, FromDefinition>(),
            Join = new Dictionary<EventType, JoinDefinition>(),
            Children = new Dictionary<string, ChildrenDefinition>(),
            All = new FromEveryDefinition(),
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
            RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap = shouldAutoMap ? (Contracts.Projections.AutoMap)AutoMap.Enabled : (Contracts.Projections.AutoMap)AutoMap.Disabled,

            // The child's own exclusions, not the parent's: a child is its own model with its own property
            // names, and the kernel matches these by bare name.
            NoAutoMapProperties = [.. NoAutoMapProperties.CollectFrom(childType, namingPolicy)]
        };
        targetChildren[propertyName] = childrenDef;
        return childrenDef;
    }

    /// <summary>
    /// childType is resolved to the concrete DerivedType implementation when unambiguous, so [SetFrom]/[SetValue]/
    /// [SetFromContext] declared on it are discovered here too. Property auto-mapping is handled by the server based
    /// on the AutoMap flag, so no explicit property mapping is needed here.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    static void MapChildType(ChildMappingScope scope)
    {
        var childType = scope.ChildType;
        var childrenDef = scope.ChildrenDefinition;
        var context = scope.Context;

        // For records, process constructor parameters to pick up attributes
        foreach (var parameter in ChildTypeInspection.GetPrimaryConstructor(childType)?.GetParameters() ?? [])
        {
            ChildParameterMappings.Apply(scope, parameter);
        }

        // Process class-level RemovedWith attributes on the child type
        ChildRemovals.Apply(context, childrenDef, childType);

        // Class-level FromEvent attributes are handed to every property, which handles SetFromContext and other attributes
        var childClassLevelFromEvents = ChildrenPropagationRules.CollectClassLevelFromEvents(childType, scope.CreatingEventType, scope.Propagation);
        foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            context.ProcessMember(childProperty, context.Definition, childClassLevelFromEvents, false, childType, childrenDef);
        }

        AddDerivedTypeDiscriminatorMapping(childrenDef.From, childType);
    }

    /// <summary>
    /// Stamps a constant discriminator mapping for a children collection whose resolved item type is
    /// adorned with <see cref="DerivedTypeAttribute"/>. AutoMap only ever wires properties that exist by
    /// name on both the event and the child type, so it can never discover <c language="csharp">_derivedTypeId</c> — that
    /// property is a serialization-time artifact added by <see cref="DerivedTypeJsonConverter{T}"/>, not a
    /// real member on the type. Without this, a newly created child of a polymorphic collection is
    /// persisted without its discriminator and cannot be resolved back to its concrete type on read.
    /// </summary>
    /// <param name="targetFrom">The From dictionary for the children definition to add the mapping to.</param>
    /// <param name="childType">The resolved item type of the children collection.</param>
    static void AddDerivedTypeDiscriminatorMapping(
        IDictionary<EventType, FromDefinition> targetFrom,
        Type childType)
    {
        var derivedTypeAttribute = childType.GetCustomAttribute<DerivedTypeAttribute>();
        if (derivedTypeAttribute is null)
        {
            return;
        }

        foreach (var fromDefinition in targetFrom.Values)
        {
            fromDefinition.Properties.TryAdd(DerivedTypeJsonConverter<object>.DerivedTypeIdProperty, $"{WellKnownExpressions.Value}({derivedTypeAttribute.Identifier})");
        }
    }
}
