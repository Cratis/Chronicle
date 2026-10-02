// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A book was put on a shelf. The shelf reaches it only through the <see cref="Book"/> event property.
/// </summary>
/// <param name="Book">The <see cref="ShelfBook"/> put on the shelf.</param>
[EventType]
public record BookShelved(ShelfBook Book);
