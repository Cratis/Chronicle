// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that adds a task to a <see cref="FluentProjectNotes"/>.
/// </summary>
/// <param name="TaskId">The task identifier.</param>
/// <param name="Title">The task title.</param>
/// <param name="Note">The task note.</param>
[EventType]
public record FluentProjectTaskAdded(Guid TaskId, string Title, string Note);
