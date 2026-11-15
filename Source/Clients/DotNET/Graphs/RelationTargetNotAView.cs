// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when the target of a relation is not a declared view.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the relation member.</param>
/// <param name="targetType">The target type.</param>
public class RelationTargetNotAView(Type viewType, string member, Type targetType)
    : InvalidGraphView(viewType, nameof(RelationTargetNotAView), $"The relation '{member}' of the view '{viewType.FullName}' leads to '{targetType.FullName}', which is not a valid view.");
