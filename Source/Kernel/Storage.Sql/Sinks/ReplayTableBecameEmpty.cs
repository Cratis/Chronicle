// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception that is thrown when a populated replay table becomes empty before promotion.
/// </summary>
/// <param name="tableName">The replay table that lost its rebuilt rows.</param>
public class ReplayTableBecameEmpty(string tableName)
    : Exception($"Replay table '{tableName}' became empty before promotion. The primary and revert tables have been left untouched.");
