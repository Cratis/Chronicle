// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model for the fluent scalar clear: a note at the root, a note on a child item, and a note on a nested
/// object, all cleared through <c>.Clear(...)</c>.
/// </summary>
/// <param name="Id">Project identifier (the event source id).</param>
/// <param name="Note">The current root note, or <see langword="null"/> when there is none.</param>
/// <param name="Summary">The nested summary, whose own note is cleared independently of the object.</param>
/// <param name="Tasks">The task items, each with a note of its own.</param>
[Passive]
public sealed record FluentProjectNotes(
    [Key] Guid Id,
    string? Note,
    FluentProjectSummary? Summary,
    IReadOnlyList<FluentProjectTask> Tasks);
