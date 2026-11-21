// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// The exception that is thrown when views or a query over them break the rules of graph views.
/// </summary>
/// <param name="issues">The <see cref="GraphViewIssue">issues</see> found.</param>
public class InvalidGraphViews(IReadOnlyList<GraphViewIssue> issues)
    : Exception($"The graph views are not valid: {string.Join("; ", issues.Select(_ => $"{_.Reason} in {_.View}{(_.Member is null ? string.Empty : $".{_.Member}")}: {_.Message}"))}")
{
    /// <summary>
    /// Gets the <see cref="GraphViewIssue">issues</see> found.
    /// </summary>
    public IReadOnlyList<GraphViewIssue> Issues { get; } = issues;
}
