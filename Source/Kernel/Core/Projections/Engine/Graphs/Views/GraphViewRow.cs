// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents one node of a view, read from the graph. A <see cref="GraphViewMemberKind.Relation"/> member holds a list of
/// <see cref="GraphViewRow"/> when many, or a single <see cref="GraphViewRow"/> or null when one.
/// </summary>
/// <param name="Values">The value of each member, by member name, in declaration order.</param>
public sealed record GraphViewRow(IReadOnlyDictionary<string, object?> Values);
