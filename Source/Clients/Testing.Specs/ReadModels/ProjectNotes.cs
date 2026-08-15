// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model whose scalar note is written by one event and cleared by another.
/// </summary>
/// <param name="Id">Project identifier (the event source id).</param>
/// <param name="Note">
/// The current note, or <see langword="null"/> when there is none. Declared nullable because that is what a clear
/// requires: "no note" is a state this member can actually hold, rather than an empty string standing in for it.
/// </param>
[Passive]
[FromEvent<ProjectNoted>]
public sealed record ProjectNotes(
    [Key] Guid Id,
    [ClearWith<ProjectNoteCleared>] string? Note);
