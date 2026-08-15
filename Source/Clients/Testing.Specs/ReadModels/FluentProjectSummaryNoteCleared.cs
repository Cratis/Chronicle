// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that clears the note on the nested summary of a <see cref="FluentProjectNotes"/>.
/// </summary>
[EventType]
public record FluentProjectSummaryNoteCleared;
