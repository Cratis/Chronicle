// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a system that discovers and provides event type migrators.
/// </summary>
public interface IEventTypeMigrators
{
    /// <summary>
    /// Gets all discovered migrator types.
    /// </summary>
    IEnumerable<Type> AllMigrators { get; }

    /// <summary>
    /// Gets all migrators for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type to get migrators for.</param>
    /// <returns>Collection of migrators.</returns>
    IEnumerable<object> GetMigratorsFor(Type eventType);
}
