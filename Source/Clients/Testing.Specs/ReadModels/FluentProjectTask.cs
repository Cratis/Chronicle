// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Child item on <see cref="FluentProjectNotes"/> holding a title and its own clearable note.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Title">The task title.</param>
/// <param name="Note">The task note, or <see langword="null"/> when it has been cleared.</param>
public record FluentProjectTask([Key] Guid Id, string Title, string? Note);
