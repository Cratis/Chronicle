// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given.sqlite;

/// <summary>
/// Runs a SQLite-specific specification only when the SQL specs select SQLite.
/// </summary>
public sealed class FactAttribute : Xunit.FactAttribute, Xunit.v3.IFactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file containing the specification.</param>
    /// <param name="sourceLineNumber">The source line containing the specification.</param>
    public FactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        var provider = Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER");
        if (provider?.Equals("SQLite", StringComparison.OrdinalIgnoreCase) == false)
        {
            Skip = "Requires the SQLite SQL specs provider.";
        }
    }
}
