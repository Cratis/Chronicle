// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given.sqlite;

/// <summary>
/// Runs a SQLite-specific specification only when the SQL specs select SQLite.
/// </summary>
public sealed class FactAttribute : Xunit.FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactAttribute"/> class.
    /// </summary>
    public FactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        var provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        if (provider?.Equals("SQLite", StringComparison.OrdinalIgnoreCase) == false)
        {
            Skip = "Requires the SQLite SQL specs provider.";
        }
    }
}
