// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when the type given for the related item of a clause is not the target view of the relation.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The relation member.</param>
/// <param name="relatedType">The type that was given.</param>
public class RelatedViewTypeMismatch(Type viewType, string member, Type relatedType)
    : Exception($"The relation '{member}' of the view '{viewType.FullName}' does not lead to the view '{relatedType.FullName}'.");
