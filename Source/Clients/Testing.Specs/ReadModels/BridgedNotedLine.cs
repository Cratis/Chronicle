// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line whose bulk list is named <c>Annotations</c> but bridged to the differently named
/// <see cref="NotedLineAdded.Notes"/> event list with <c>[SetFrom]</c> — the sanctioned fix for a rename
/// that avoids the silent AutoMap-to-nothing failure without renaming the read-model property.
/// </summary>
/// <param name="LineNumber">The line number, used as the child key.</param>
/// <param name="Description">The line description.</param>
/// <param name="Annotations">A list explicitly mapped from the event's differently named <c>Notes</c> list.</param>
public record BridgedNotedLine(
    [Key] string LineNumber,
    string Description,

    [SetFrom<NotedLineAdded>(nameof(NotedLineAdded.Notes))]
    IReadOnlyList<LineNote> Annotations);
