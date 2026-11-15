// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a rule a graph view declaration or query breaks.
/// </summary>
/// <param name="Reason">The stable reason code, one of <see cref="GraphViewReason"/>.</param>
/// <param name="View">The name of the view the issue is in.</param>
/// <param name="Member">The name of the member the issue is in, when it is in one.</param>
/// <param name="Message">A message that says what is wrong.</param>
public record GraphViewIssue(string Reason, string View, string? Member, string Message);
