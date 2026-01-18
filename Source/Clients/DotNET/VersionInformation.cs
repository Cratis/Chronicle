// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Provides version information from the Chronicle client assembly.
/// </summary>
public static class VersionInformation
{
    /// <summary>
    /// Gets the version from the calling assembly's AssemblyInformationalVersion or AssemblyVersion.
    /// </summary>
    /// <returns>The version string.</returns>
    public static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informationalVersion is not null)
        {
            var version = informationalVersion.InformationalVersion;
            var plusIndex = version.IndexOf('+');
            return plusIndex > 0 ? version[..plusIndex] : version;
        }

        var assemblyVersion = assembly.GetName().Version;
        return assemblyVersion?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// Gets the commit SHA from the calling assembly's AssemblyInformationalVersion metadata.
    /// </summary>
    /// <returns>The commit SHA if available, otherwise "[N/A]".</returns>
    public static string GetCommitSha()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informationalVersion is not null)
        {
            var version = informationalVersion.InformationalVersion;
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0 && plusIndex < version.Length - 1)
            {
                return version[(plusIndex + 1)..];
            }
        }

        return "[N/A]";
    }
}
