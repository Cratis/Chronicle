// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when the <see cref="ChildrenFromAttribute{TEvent}"/> attributes on a member declare
/// different orderings for the same children collection.
/// </summary>
/// <param name="memberName">The name of the children member.</param>
public class ConflictingChildrenOrdering(string memberName)
    : Exception($"The children member '{memberName}' declares more than one ordering. Use only one of Prepend, OrderBy or OrderByDescending, and declare the same ordering on every [ChildrenFrom] for the member.");
