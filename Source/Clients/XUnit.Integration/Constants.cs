// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents constants for the integration tests.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Gets the name of the event store.
    /// </summary>
    public const string EventStore = "testing";

    /// <summary>
    /// Gets the name of the read models database.
    /// </summary>
    public const string ReadModelsDatabaseName = EventStore;

    /// <summary>
    /// Gets the name of the event store database.
    /// </summary>
    public const string EventStoreDatabaseName = $"{EventStore}+es";

    /// <summary>
    /// Gets the name of the event store namespace database.
    /// </summary>
    public const string EventStoreNamespaceDatabaseName = $"{EventStore}+es+Default";
}
