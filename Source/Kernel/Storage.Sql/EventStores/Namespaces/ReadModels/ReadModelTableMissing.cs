// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Exception that gets thrown when a table that must already exist is not there.
/// </summary>
#pragma warning disable CA1064 // Internal accessor diagnostic, not a public client contract.
internal sealed class ReadModelTableMissing : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTableMissing"/> class.
    /// </summary>
    /// <param name="tableName">The physical table name.</param>
    internal ReadModelTableMissing(string tableName)
        : base($"The read model table '{tableName}' is required to exist and does not.")
    {
    }
}
