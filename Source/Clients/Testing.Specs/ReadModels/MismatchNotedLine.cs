// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line whose bulk list is named <c>Annotations</c> — deliberately different from the
/// <see cref="NotedLineAdded.Notes"/> list on the event — to reproduce the silent AutoMap-to-nothing failure.
/// </summary>
/// <param name="LineNumber">The line number, used as the child key.</param>
/// <param name="Description">The line description.</param>
/// <param name="Annotations">A list whose name does not match any event property, so it projects empty.</param>
public record MismatchNotedLine(
    [Key] string LineNumber,
    string Description,
    IReadOnlyList<LineNote> Annotations);
