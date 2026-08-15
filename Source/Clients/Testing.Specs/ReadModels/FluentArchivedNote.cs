// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model for the older <c>.Set(...).ToValue(...)</c> spelling: one member cleared with a null, one set to a
/// real constant. The pair keeps both halves of that method honest.
/// </summary>
/// <param name="Id">Project identifier (the event source id).</param>
/// <param name="Note">The note, cleared through <c>ToValue(null)</c>.</param>
/// <param name="Status">The status, set through <c>ToValue</c> with a real constant.</param>
[Passive]
public sealed record FluentArchivedNote(
    [Key] Guid Id,
    string? Note,
    string? Status);
