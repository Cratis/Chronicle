// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents constants for the integration tests.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Gets the prefix for the event store names.
    /// </summary>
    public const string EventStorePrefix = "testing";

    /// <summary>
    /// Gets the name of the event store for the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The unique identifier for the fixture.</param>
    /// <returns>The event store name.</returns>
    public static string GetEventStore(string uniqueId) => $"{EventStorePrefix}{uniqueId}";

    /// <summary>
    /// Gets the name of the read models database for the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The unique identifier for the fixture.</param>
    /// <returns>The read models database name.</returns>
    public static string GetReadModelsDatabaseName(string uniqueId) => GetEventStore(uniqueId);

    /// <summary>
    /// Gets the name of the event store database for the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The unique identifier for the fixture.</param>
    /// <returns>The event store database name.</returns>
    public static string GetEventStoreDatabaseName(string uniqueId) => $"{GetEventStore(uniqueId)}-es";

    /// <summary>
    /// Gets the name of the event store namespace database for the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The unique identifier for the fixture.</param>
    /// <returns>The event store namespace database name.</returns>
    public static string GetEventStoreNamespaceDatabaseName(string uniqueId) => $"{GetEventStore(uniqueId)}-es+Default";
}
