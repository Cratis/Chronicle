// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Internal class for holding all database names.
/// </summary>
public static class WellKnownDatabaseNames
{
    /// <summary>
    /// The collection that holds <see cref="EventStore"/>.
    /// </summary>
    public const string Chronicle = "chronicle+main";
}
