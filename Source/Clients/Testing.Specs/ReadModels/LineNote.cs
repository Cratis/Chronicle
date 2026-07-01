// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A plain record element used as the item type of a bulk list carried on a child read model, to verify
/// that a whole <see cref="IReadOnlyList{T}"/> set from a single child-creating event materializes on a
/// <c>[ChildrenFrom]</c> child.
/// </summary>
/// <param name="Text">The note text.</param>
/// <param name="Order">The ordinal of the note.</param>
public record LineNote(string Text, int Order);
