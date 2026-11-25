// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a view contains itself, directly or through other views.
/// </summary>
/// <param name="viewType">The type of the view.</param>
public class RecursiveView(Type viewType)
    : InvalidGraphView(viewType, nameof(RecursiveView), $"The view '{viewType.FullName}' contains itself, directly or through other views. Nesting is finite: declare a separate view for each level.");
