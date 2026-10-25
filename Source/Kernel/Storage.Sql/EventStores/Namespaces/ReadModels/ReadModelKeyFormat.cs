// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Describes how the composite primary key of one physical read-model table is encoded.
/// </summary>
public enum ReadModelKeyFormat
{
    /// <summary>
    /// The key parts are joined with an underscore. Two different keys can produce the same id.
    /// This is the format of every table that has no recorded format.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// The table is a replay table being filled with the collision-free "~k1:" encoding.
    /// </summary>
    BuildingK1 = 1,

    /// <summary>
    /// The key is stored with the collision-free "~k1:" encoding.
    /// </summary>
    K1 = 2
}
