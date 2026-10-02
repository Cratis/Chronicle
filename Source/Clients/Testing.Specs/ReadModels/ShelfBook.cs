// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A book on a <see cref="Shelf"/>.
/// </summary>
/// <param name="Isbn">Identifies the book on the shelf.</param>
/// <param name="Title">The title of the book.</param>
public record ShelfBook(string Isbn, string Title);
