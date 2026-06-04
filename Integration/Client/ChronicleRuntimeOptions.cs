// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

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
    /// Parse runtime options from multiple sources, in priority order:
    /// 1. Environment variables (<c>CHRONICLE_RUNTIME_MODE</c>, <c>CHRONICLE_STORAGE_PROVIDER</c>).
    /// 2. The parent vstest process command line, which contains the original
    ///    <c>dotnet test -- inprocess mongodb</c> arguments that vstest receives but does not
    ///    forward to the test host's own <c>GetCommandLineArgs()</c>.
    /// 3. Own process command line (for direct executable invocation, not via <c>dotnet test</c>).
    /// </summary>
    /// <returns>Parsed options with defaults when arguments are not provided.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when InProcess mode is selected with a non-MongoDB storage provider.
    /// </exception>
    public static ChronicleRuntimeOptions Parse()
    {
        var mode = ChronicleRuntimeMode.OutOfProcess;
        var storageProvider = ChronicleStorageProvider.MongoDB;

        // 1. Environment variables have highest priority.
        //    These are set by `dotnet test --environment KEY=VALUE` or by the shell.
        ApplyArgs(GetEnvironmentArgs(), ref mode, ref storageProvider);

        // 2. Parent process (vstest) command line.
        //    `dotnet test -- inprocess mongodb` passes the extra args to vstest but vstest
        //    does NOT forward them to the testhost process. Reading vstest's own command
        //    line via the OS gives us the original args.
        ApplyArgs(GetParentProcessSeparatorArgs(), ref mode, ref storageProvider);

        // 3. Own GetCommandLineArgs() fallback.
        //    Only applies when the test host is invoked directly without dotnet test.
        ApplyArgs(GetOwnProcessArgs(), ref mode, ref storageProvider);

        if (mode == ChronicleRuntimeMode.InProcess && storageProvider != ChronicleStorageProvider.MongoDB)
        {
            throw new InvalidOperationException($"InProcess mode supports only MongoDB storage. Selected: {storageProvider}.");
        }

        return new(mode, storageProvider);
    }

    static string[] GetEnvironmentArgs()
    {
        var result = new List<string>();
        var envMode = Environment.GetEnvironmentVariable("CHRONICLE_RUNTIME_MODE");
        if (envMode is not null)
        {
            result.Add(envMode);
        }

        var envStorage = Environment.GetEnvironmentVariable("CHRONICLE_STORAGE_PROVIDER");
        if (envStorage is not null)
        {
            result.Add(envStorage);
        }

        return [.. result];
    }

    static string[] GetParentProcessSeparatorArgs()
    {
        try
        {
            // The testhost process has --parentprocessid <pid> in its args when launched by vstest.
            var myArgs = Environment.GetCommandLineArgs();
            var idx = Array.IndexOf(myArgs, "--parentprocessid");
            if (idx < 0 || idx + 1 >= myArgs.Length)
            {
                return [];
            }

            if (!int.TryParse(myArgs[idx + 1], out var parentPid))
            {
                return [];
            }

            var parentCmdLine = ReadProcessCommandLine(parentPid);
            if (string.IsNullOrWhiteSpace(parentCmdLine))
            {
                return [];
            }

            // Find the -- separator in the parent's command line and return everything after it.
            var parts = parentCmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sepIdx = Array.IndexOf(parts, "--");
            if (sepIdx < 0)
            {
                return [];
            }

            return parts[(sepIdx + 1)..];
        }
        catch
        {
            return [];
        }
    }

    static string ReadProcessCommandLine(int pid)
    {
        // Linux: /proc/{pid}/cmdline contains null-delimited args
        var procFile = $"/proc/{pid}/cmdline";
        if (File.Exists(procFile))
        {
            var bytes = File.ReadAllBytes(procFile);
            return System.Text.Encoding.UTF8.GetString(bytes).Replace('\0', ' ').Trim();
        }

        // macOS / other Unix: use ps
        try
        {
            var psi = new ProcessStartInfo("ps", $"-p {pid} -o args=")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(psi);
            if (process is null)
            {
                return string.Empty;
            }

            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }

    static string[] GetOwnProcessArgs()
    {
        var args = Environment.GetCommandLineArgs().Select(_ => _.Trim()).ToArray();
        return args.Skip(1).Where(_ => !string.IsNullOrWhiteSpace(_)).ToArray();
    }

    static void ApplyArgs(string[] args, ref ChronicleRuntimeMode mode, ref ChronicleStorageProvider storageProvider)
    {
        if (args.Length >= 1 && TryParseMode(args[0], out var parsedMode))
        {
            mode = parsedMode;
        }

        if (args.Length >= 2 && TryParseStorageProvider(args[1], out var parsedProvider))
        {
            storageProvider = parsedProvider;
        }

        foreach (var argument in args)
        {
            if (argument.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase) &&
                TryParseMode(argument["--mode=".Length..], out parsedMode))
            {
                mode = parsedMode;
            }

            if (argument.StartsWith("--database=", StringComparison.OrdinalIgnoreCase) &&
                TryParseStorageProvider(argument["--database=".Length..], out parsedProvider))
            {
                storageProvider = parsedProvider;
            }

            if (argument.StartsWith("--db=", StringComparison.OrdinalIgnoreCase) &&
                TryParseStorageProvider(argument["--db=".Length..], out parsedProvider))
            {
                storageProvider = parsedProvider;
            }
        }
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
