// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a type is used as a view but is not a valid view.
/// </summary>
/// <param name="type">The type.</param>
/// <param name="reason">Why the view was left out, if it was declared and refused.</param>
public class NotAGraphView(Type type, Exception? reason)
    : Exception(
        reason is null
            ? $"The type '{type.FullName}' is not a view. Mark it with ViewOf<TNode> and make sure the client artifacts discover it."
            : $"The type '{type.FullName}' is not a valid view: {reason.Message}",
        reason);
