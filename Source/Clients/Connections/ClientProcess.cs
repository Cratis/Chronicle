// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the identifying information about the running client process that travels with a
/// connection - so the server (and its Workbench) can tell connected client instances apart.
/// </summary>
internal static class ClientProcess
{
    /// <summary>
    /// Gets the version of the client application - the entry assembly's informational version
    /// without the '+' build metadata, falling back to its assembly version.
    /// </summary>
    public static string Version { get; } = GetEntryAssemblyVersion();

    /// <summary>
    /// Gets the identifier of the running process.
    /// </summary>
    public static int Id => Environment.ProcessId;

    /// <summary>
    /// Gets the full path of the running process executable.
    /// </summary>
    public static string Path => Environment.ProcessPath ?? string.Empty;

    /// <summary>
    /// Gets the name of the machine the process is running on.
    /// </summary>
    public static string MachineName => Environment.MachineName;

    static string GetEntryAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
        {
            return string.Empty;
        }

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informationalVersion is not null)
        {
            var version = informationalVersion.InformationalVersion;
            var plusIndex = version.IndexOf('+');
            return plusIndex > 0 ? version[..plusIndex] : version;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
