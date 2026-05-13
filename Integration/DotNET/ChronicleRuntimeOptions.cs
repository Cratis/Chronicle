// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents the runtime mode for integration specifications.
/// </summary>
public enum ChronicleRuntimeMode
{
    /// <summary>
    /// Runs against a Chronicle container.
    /// </summary>
    OutOfProcess = 0,

    /// <summary>
    /// Runs with Kernel in-process and only MongoDB in a container.
    /// </summary>
    InProcess = 1,
}

/// <summary>
/// Represents supported storage providers for integration specifications.
/// </summary>
public enum ChronicleStorageProvider
{
    /// <summary>
    /// MongoDB storage provider.
    /// </summary>
    MongoDB = 0,

    /// <summary>
    /// PostgreSQL storage provider.
    /// </summary>
    PostgreSql = 1,

    /// <summary>
    /// Microsoft SQL Server storage provider.
    /// </summary>
    MsSql = 2,

    /// <summary>
    /// SQLite storage provider.
    /// </summary>
    Sqlite = 3,
}

/// <summary>
/// Runtime options for integration specifications.
/// </summary>
/// <param name="Mode">The selected runtime mode.</param>
/// <param name="StorageProvider">The selected storage provider.</param>
public record ChronicleRuntimeOptions(
    ChronicleRuntimeMode Mode,
    ChronicleStorageProvider StorageProvider)
{
    /// <summary>
    /// Parse runtime options from command line arguments.
    /// </summary>
    /// <returns>Parsed options with defaults when arguments are not provided.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when InProcess mode is selected with a non-MongoDB storage provider.
    /// </exception>
    public static ChronicleRuntimeOptions Parse()
    {
        var args = Environment.GetCommandLineArgs().Select(_ => _.Trim()).ToArray();
        var mode = ChronicleRuntimeMode.OutOfProcess;
        var storageProvider = ChronicleStorageProvider.MongoDB;

        // Positional arguments:
        //   arg1 => mode: inprocess|outofprocess
        //   arg2 => database: mongodb|postgresql|mssql|sqlite
        var positional = args
            .Skip(1)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .ToArray();

        if (positional.Length >= 1 && TryParseMode(positional[0], out var parsedMode))
        {
            mode = parsedMode;
        }

        if (positional.Length >= 2 && TryParseStorageProvider(positional[1], out var parsedProvider))
        {
            storageProvider = parsedProvider;
        }

        // Named arguments override positional:
        //   --mode=inprocess|outofprocess
        //   --database=mongodb|postgresql|mssql|sqlite
        foreach (var argument in positional)
        {
            if (argument.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase) &&
                TryParseMode(argument["--mode=".Length..], out parsedMode))
            {
                mode = parsedMode;
            }

            if ((argument.StartsWith("--database=", StringComparison.OrdinalIgnoreCase) ||
                 argument.StartsWith("--db=", StringComparison.OrdinalIgnoreCase)) &&
                TryParseStorageProvider(argument[(argument.IndexOf('=') + 1)..], out parsedProvider))
            {
                storageProvider = parsedProvider;
            }
        }

        if (mode == ChronicleRuntimeMode.InProcess && storageProvider != ChronicleStorageProvider.MongoDB)
        {
            throw new InvalidOperationException("InProcess mode supports only MongoDB storage.");
        }

        return new(mode, storageProvider);
    }

    static bool TryParseMode(string value, out ChronicleRuntimeMode mode)
    {
        if (value.Equals("inprocess", StringComparison.OrdinalIgnoreCase))
        {
            mode = ChronicleRuntimeMode.InProcess;
            return true;
        }

        if (value.Equals("outofprocess", StringComparison.OrdinalIgnoreCase))
        {
            mode = ChronicleRuntimeMode.OutOfProcess;
            return true;
        }

        mode = default;
        return false;
    }

    static bool TryParseStorageProvider(string value, out ChronicleStorageProvider provider)
    {
        if (value.Equals("mongodb", StringComparison.OrdinalIgnoreCase))
        {
            provider = ChronicleStorageProvider.MongoDB;
            return true;
        }

        if (value.Equals("postgresql", StringComparison.OrdinalIgnoreCase) || value.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            provider = ChronicleStorageProvider.PostgreSql;
            return true;
        }

        if (value.Equals("mssql", StringComparison.OrdinalIgnoreCase) || value.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            provider = ChronicleStorageProvider.MsSql;
            return true;
        }

        if (value.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            provider = ChronicleStorageProvider.Sqlite;
            return true;
        }

        provider = default;
        return false;
    }
}
