// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Nested object on <see cref="FluentProjectNotes"/> holding a headline and its own clearable note.
/// </summary>
/// <param name="Headline">The summary headline.</param>
/// <param name="Note">The summary note, or <see langword="null"/> when it has been cleared.</param>
public record FluentProjectSummary(string Headline, string? Note);
