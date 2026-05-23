// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

// Primary-constructor parameter 'database' is only read inside the #if DEVELOPMENT branch of
// Reset(); Release builds intentionally leave it unread.
#pragma warning disable CS9113

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Wipes the SQL backing store between integration test specs without restarting the container.
/// Delegates to <c>IDatabase.Wipe</c> so that the file deletion / table truncation and the
/// migration-cache invalidation always happen together. The wipe path is only compiled when the
/// <c>DEVELOPMENT</c> preprocessor symbol is defined; production builds throw
/// <see cref="NotSupportedException"/>, matching the gate on <c>IDatabase.Wipe</c>.
/// </summary>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> describing the active storage backend.</param>
/// <param name="database">The <see cref="IDatabase"/> that performs the wipe.</param>
public class SqlKernelStateResetHandler(IOptions<ChronicleOptions> options, IDatabase database) : ICanPerformKernelStateReset
{
    /// <inheritdoc/>
    public bool CanReset()
    {
        var type = options.Value.Storage.Type;
        return string.Equals(type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public Task Reset()
    {
#if DEVELOPMENT
        return database.Wipe();
#else
        throw new NotSupportedException(
            "SqlKernelStateResetHandler.Reset is only available when Storage.Sql is compiled with the DEVELOPMENT preprocessor symbol.");
#endif
    }
}
