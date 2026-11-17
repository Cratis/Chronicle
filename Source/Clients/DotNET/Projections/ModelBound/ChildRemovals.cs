// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Applies <c language="csharp">[RemovedWith]</c> and <c language="csharp">[RemovedWithJoin]</c> attributes to a children definition.
/// </summary>
static class ChildRemovals
{
    /// <summary>
    /// Adds the removal triggers declared by the attributes of a provider to a children definition.
    /// </summary>
    /// <param name="context">The shared processing collaborators.</param>
    /// <param name="target">The children definition that is removed by the triggers.</param>
    /// <param name="source">The type, parameter or property carrying the attributes.</param>
    internal static void Apply(ChildrenProcessingContext context, ChildrenDefinition target, ICustomAttributeProvider source)
    {
        foreach (var (attr, eventType) in source.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            target.RemovedWith.ProcessRemovedWithAttribute(context.GetOrCreateEventType, context.NamingPolicy, attr, eventType);
        }

        foreach (var (attr, eventType) in source.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            target.RemovedWithJoin.ProcessRemovedWithJoinAttribute(context.GetOrCreateEventType, context.NamingPolicy, attr, eventType);
        }
    }
}
