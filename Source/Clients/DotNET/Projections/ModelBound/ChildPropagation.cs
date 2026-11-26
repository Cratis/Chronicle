// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Describes which events are propagated into a children collection when its item type refers back to itself.
/// </summary>
/// <param name="IncludeSelfReferencingEvents">Whether events that also create children of the item type are propagated.</param>
/// <param name="AncestorCreatorEventTypes">Events that create children of an ancestor collection, which must never be propagated.</param>
internal sealed record ChildPropagation(
    bool IncludeSelfReferencingEvents,
    IReadOnlySet<Type>? AncestorCreatorEventTypes)
{
    /// <summary>
    /// Gets the propagation used for a collection that is not self-referential.
    /// </summary>
    internal static ChildPropagation None { get; } = new(false, null);
}
