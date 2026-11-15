// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a rule a view or a query over views of a graph preview breaks.
/// </summary>
/// <param name="Reason">The stable reason code, such as UnknownEdge or TargetLabelMismatch.</param>
/// <param name="View">The name of the view the error is in.</param>
/// <param name="Member">The name of the member the error is in, if it is in one.</param>
/// <param name="Message">A message that says what is wrong.</param>
public record GraphViewError(string Reason, string View, string? Member, string Message);
