// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Exception that gets thrown when a SQL sink is created over a database that cannot resolve read model table names.
/// </summary>
#pragma warning disable CA1064 // Internal SQL registration diagnostic, not a public client contract.
internal sealed class ReadModelTablesUnavailable : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTablesUnavailable"/> class.
    /// </summary>
    internal ReadModelTablesUnavailable()
        : base("The database does not resolve read model table names. Use the SQL Database registered by WithSql.")
    {
    }
}
