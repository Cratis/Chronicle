// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line on a <see cref="NotedOrder"/> that carries a whole bulk list of notes, set from a single
/// child-creating event.
/// </summary>
/// <param name="LineNumber">The line number, used as the child key.</param>
/// <param name="Description">The line description.</param>
/// <param name="Notes">The whole list of notes for the line.</param>
/// <param name="Tags">The whole list of bare-concept tags for the line.</param>
public record NotedLine(
    [Key] string LineNumber,
    string Description,
    IReadOnlyList<LineNote> Notes,
    IReadOnlyList<ConceptListItem> Tags);
