// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that adds a line to an order, carrying the whole set of notes for the line in a single event.
/// </summary>
/// <param name="LineNumber">The line number, used as the child key.</param>
/// <param name="Description">The line description.</param>
/// <param name="Notes">The whole list of notes for the line.</param>
/// <param name="Tags">The whole list of bare-concept tags for the line.</param>
[EventType]
public record NotedLineAdded(
    string LineNumber,
    string Description,
    IReadOnlyList<LineNote> Notes,
    IReadOnlyList<ConceptListItem> Tags);
