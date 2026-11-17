// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Walks the members of a child type to discover the <c language="csharp">[ChildrenFrom]</c> and
/// <c language="csharp">[Nested]</c> declarations that sit one level further down.
/// </summary>
/// <param name="context">The shared processing collaborators.</param>
/// <param name="parentChildrenDef">The children definition of the child type being walked.</param>
/// <param name="childType">The child type being walked.</param>
/// <param name="visitedChildTypes">Child types already visited in the current recursion chain.</param>
sealed class NestedChildrenWalker(
    ChildrenProcessingContext context,
    ChildrenDefinition parentChildrenDef,
    Type childType,
    HashSet<Type> visitedChildTypes)
{
    /// <summary>
    /// Processes the nested collections and nested objects declared on a child type, unless the type was already visited.
    /// </summary>
    /// <param name="childType">The child type to walk.</param>
    /// <param name="context">The shared processing collaborators.</param>
    /// <param name="parentChildrenDef">The children definition of the child type.</param>
    /// <param name="visitedChildTypes">Child types already visited in the current recursion chain, used to break cycles.</param>
    internal static void Walk(Type childType, ChildrenProcessingContext context, ChildrenDefinition parentChildrenDef, HashSet<Type> visitedChildTypes)
    {
        if (visitedChildTypes.Add(childType))
        {
            new NestedChildrenWalker(context, parentChildrenDef, childType, visitedChildTypes).WalkMembers();
        }
    }

    void WalkMembers()
    {
        // Constructor parameters take precedence over the properties they back
        var parameters = ChildTypeInspection.GetPrimaryConstructor(childType)?.GetParameters() ?? [];
        var parameterNames = new HashSet<string>(parameters.Select(parameter => parameter.Name!), StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters)
        {
            WalkMember(new(parameter.Name!, parameter.ParameterType, parameter, parameter.IsDefined(typeof(NestedAttribute), inherit: false)));
        }

        var properties = childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => !parameterNames.Contains(property.Name));

        foreach (var property in properties)
        {
            WalkMember(new(property.Name, property.PropertyType, property, Attribute.IsDefined(property, typeof(NestedAttribute))));
        }
    }

    void WalkMember(Member member)
    {
        foreach (var (attribute, eventType) in member.Attributes.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
        {
            parentChildrenDef.ProcessChildrenFromAttributeForNestedChildren(
                context,
                new(member.Name, member.Type, attribute, eventType, childType),
                visitedChildTypes);

            RouteRemovedWith(member);
        }

        if (member.IsNested)
        {
            parentChildrenDef.ProcessNestedAttributeForChildren(
                context.GetOrCreateEventType,
                context.NamingPolicy,
                member.Name,
                member.Type,
                member.Attributes.GetAttributesOfGenericType<ClearWithAttribute<object>>().Select(_ => _.EventType),
                context.ProcessMember,
                context.Definition,
                childType);
        }
    }

    /// <summary>
    /// A [RemovedWith] alongside the [ChildrenFrom] on this nested collection removes the keyed grandchild, so route
    /// it onto the nested child definition just created — not onto this parent child definition, where it would
    /// remove the whole parent item.
    /// </summary>
    /// <param name="member">The member carrying the attributes.</param>
    void RouteRemovedWith(Member member)
    {
        var nestedChildName = (string)ProjectionPropertyPaths.Resolve(member.Name, context.NamingPolicy, ProjectionPathRole.Target);
        if (parentChildrenDef.Children.TryGetValue(nestedChildName, out var nestedChildDef))
        {
            ChildRemovals.Apply(context, nestedChildDef, member.Attributes);
        }
    }

    sealed record Member(string Name, Type Type, ICustomAttributeProvider Attributes, bool IsNested);
}
