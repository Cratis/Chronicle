// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Identifies the purpose of a logical read model table.
/// </summary>
internal enum ReadModelTableRole
{
    /// <summary>The currently served read model.</summary>
    Live = 0,

    /// <summary>The shadow being rebuilt by replay.</summary>
    Replay = 1,

    /// <summary>A retained pre-replay read model.</summary>
    Backup = 2
}
