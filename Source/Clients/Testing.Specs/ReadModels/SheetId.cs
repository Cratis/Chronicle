// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Identifier for a <see cref="Sheet"/>.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SheetId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="SheetId"/>.
    /// </summary>
    public static readonly SheetId NotSet = new(Guid.Empty);

    /// <summary>
    /// Creates a new <see cref="SheetId"/>.
    /// </summary>
    /// <returns>A new <see cref="SheetId"/>.</returns>
    public static SheetId New() => new(Guid.NewGuid());

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="SheetId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator SheetId(Guid value) => new(value);
}
