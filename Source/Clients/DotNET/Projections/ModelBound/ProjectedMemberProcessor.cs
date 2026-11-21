// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Processes the projection attributes of a single read model member - a record constructor parameter or a
/// property - into the projection definition.
/// </summary>
/// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
/// <param name="namingPolicy">The naming policy for converting property names.</param>
/// <param name="processMember">The action to process child members recursively.</param>
internal sealed class ProjectedMemberProcessor(
    Func<Type, EventType> getOrCreateEventType,
    INamingPolicy namingPolicy,
    Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> processMember)
{
    readonly MemberEventMappings _eventMappings = new(getOrCreateEventType, namingPolicy);

    /// <summary>
    /// Processes the member.
    /// </summary>
    /// <param name="unresolved">The <see cref="MemberProcessingScope"/> of the member, before its property name is resolved.</param>
    internal void Process(MemberProcessingScope unresolved)
    {
        var scope = unresolved with
        {
            PropertyName = (string)ProjectionPropertyPaths.Resolve(unresolved.Member.Name, namingPolicy, ProjectionPathRole.Target)
        };

        _eventMappings.Apply(scope);
        ProcessJoins(scope);
        ProcessRemovals(scope);

        // Only process ChildrenFromAttribute and NestedAttribute when at root level.
        // For nested children, ProcessNestedChildren handles adding them to the correct parent ChildrenDefinition.
        if (scope.IsRoot)
        {
            ProcessChildren(scope);
            ProcessNested(scope);
        }

        _eventMappings.ApplyClassLevelFromEvents(scope);
        ProcessFromEvery(scope);
    }

    void ProcessJoins(MemberProcessingScope scope)
    {
        foreach (var (attr, eventType) in scope.Member.Provider.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            scope.Join.ProcessJoinAttribute(getOrCreateEventType, namingPolicy, attr, eventType, scope.Member.Name, scope.PropertyName, scope.ChildrenDefinition?.IdentifiedBy);
        }
    }

    void ProcessRemovals(MemberProcessingScope scope)
    {
        // A [RemovedWith] that sits alongside a [ChildrenFrom] on the same collection property means "remove the
        // keyed child", so it must land on that collection's own child definition, never on the containing
        // definition - where it would remove the whole document (at root) or the whole parent item (nested).
        // A root children collection is re-homed by ProcessChildren; a nested one is re-homed by ProcessNestedChildren.
        // A [RemovedWith] on any other property keeps its containing-document-removal meaning.
        if (scope.IsChildrenCollection)
        {
            return;
        }

        ApplyRemovals(scope.Member, scope.RemovedWith, scope.RemovedWithJoin);
    }

    void ApplyRemovals(ProjectedMember member, IDictionary<EventType, RemovedWithDefinition> removedWith, IDictionary<EventType, RemovedWithJoinDefinition> removedWithJoin)
    {
        foreach (var (attr, eventType) in member.Provider.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            removedWith.ProcessRemovedWithAttribute(getOrCreateEventType, namingPolicy, attr, eventType);
        }

        foreach (var (attr, eventType) in member.Provider.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            removedWithJoin.ProcessRemovedWithJoinAttribute(getOrCreateEventType, namingPolicy, attr, eventType);
        }
    }

    void ProcessChildren(MemberProcessingScope scope)
    {
        var member = scope.Member;
        foreach (var (attr, eventType) in member.Provider.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
        {
            scope.Definition.ProcessChildrenFromAttribute(getOrCreateEventType, namingPolicy, member.Name, member.MemberType, attr, eventType, processMember, scope.ModelType);
        }

        // Route the member-level [RemovedWith] / [RemovedWithJoin] to the child definition just created,
        // so cancelling a keyed child removes that one child rather than the whole document.
        if (scope.IsChildrenCollection && scope.Definition.Children.TryGetValue(scope.PropertyName, out var childrenDefinition))
        {
            ApplyRemovals(member, childrenDefinition.RemovedWith, childrenDefinition.RemovedWithJoin);
        }
    }

    void ProcessNested(MemberProcessingScope scope)
    {
        var member = scope.Member;
        if (!member.IsNested)
        {
            return;
        }

        scope.Definition.ProcessNestedAttribute(
            getOrCreateEventType,
            namingPolicy,
            member.Name,
            member.MemberType,
            member.Provider.GetAttributesOfGenericType<ClearWithAttribute<object>>().Select(_ => _.EventType),
            processMember,
            scope.ModelType);
    }

    void ProcessFromEvery(MemberProcessingScope scope)
    {
        var fromEvery = scope.Member.GetAttribute<FromEveryAttribute>();
        if (fromEvery is not null)
        {
            ModelBoundProjectionBuilder.AddFromEveryMapping(scope.All, namingPolicy, scope.PropertyName, fromEvery);
        }

        var fromAll = scope.Member.GetAttribute<FromAllAttribute>();
        if (fromAll is null)
        {
            return;
        }

        ModelBoundProjectionBuilder.AddFromEveryMapping(scope.All, namingPolicy, scope.PropertyName, new FromEveryAttribute(property: fromAll.Property, contextProperty: fromAll.ContextProperty));
        if (scope.IsRoot)
        {
            scope.Definition.SubscribesToAllEvents = true;
        }
    }
}
