// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Defines a DbContext that manages a dynamically named table.
/// </summary>
public interface ITableDbContext
{
    /// <summary>
    /// Ensures the table exists in the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureTableExists();
}
