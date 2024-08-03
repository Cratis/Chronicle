// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for constraints.
/// </summary>
public interface IConstraintsStorage
{
    /// <summary>
    /// Gets the storage for unique constraints.
    /// </summary>
    IUniqueConstraintsStorage Unique { get; }

    /// <summary>
    /// Gets the storage for unique event type constraints.
    /// </summary>
    IUniqueEventTypesConstraintsStorage UniqueEventTypes { get; }
}
