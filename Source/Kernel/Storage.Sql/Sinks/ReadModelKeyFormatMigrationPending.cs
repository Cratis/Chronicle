// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception that is thrown when a write targets a composite-key read model table that still uses the legacy key format.
/// </summary>
/// <param name="tableName">The table that needs to be rebuilt by a replay.</param>
public class ReadModelKeyFormatMigrationPending(string tableName)
    : Exception($"Read model table '{tableName}' stores composite keys in the legacy format. Writes are refused until a replay rebuilds the table with the current key format.");
