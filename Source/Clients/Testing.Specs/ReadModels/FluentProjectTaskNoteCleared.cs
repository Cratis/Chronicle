// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that clears the note on one task of a <see cref="FluentProjectNotes"/>.
/// </summary>
/// <param name="TaskId">The task whose note is cleared.</param>
[EventType]
public record FluentProjectTaskNoteCleared(Guid TaskId);
