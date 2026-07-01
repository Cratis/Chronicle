// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents an append definition for a capture.
/// </summary>
/// <param name="EventType">The event type to append.</param>
/// <param name="When">The append condition.</param>
/// <param name="FieldAssignments">The field assignments for the event payload.</param>
public record AppendDefinition(
    string EventType,
    WhenClause When,
    IReadOnlyDictionary<string, string> FieldAssignments);
