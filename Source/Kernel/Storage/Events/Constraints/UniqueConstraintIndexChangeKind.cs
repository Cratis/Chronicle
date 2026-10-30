// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the kinds of change an append makes to a unique constraint index.
/// </summary>
public enum UniqueConstraintIndexChangeKind
{
    /// <summary>
    /// The event source claims a value, see <see cref="IUniqueConstraintsStorage.Save"/>.
    /// </summary>
    Save = 0,

    /// <summary>
    /// The event source releases every value it holds, see <see cref="IUniqueConstraintsStorage.Remove"/>.
    /// </summary>
    Remove = 1,

    /// <summary>
    /// The event source releases one value it holds, see <see cref="IUniqueConstraintsStorage.RemoveValue"/>.
    /// </summary>
    RemoveValue = 2
}
