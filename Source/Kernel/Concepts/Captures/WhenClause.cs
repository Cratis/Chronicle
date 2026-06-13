// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the condition for appending an event.
/// </summary>
/// <param name="Type">The <see cref="WhenClauseType"/>.</param>
/// <param name="Properties">Properties participating in the condition.</param>
/// <param name="FromValue">Optional source value for transitions.</param>
/// <param name="ToValue">Optional target value for transitions.</param>
/// <param name="Expression">Optional raw expression.</param>
public record WhenClause(
    WhenClauseType Type,
    IReadOnlyList<string> Properties,
    string? FromValue = default,
    string? ToValue = default,
    string? Expression = default);
