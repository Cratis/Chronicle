// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Provides version information from the entry assembly (the consuming application).
/// This allows Chronicle to track which version of the consumer application is running.
/// </summary>
public static class VersionInformation
{
    /// <summary>
    /// Gets the version from the entry assembly's AssemblyInformationalVersion or AssemblyVersion.
    /// The entry assembly is the application using Chronicle, not Chronicle itself.
    /// If the assembly has an <see cref="AssemblyInformationalVersionAttribute"/>, the version portion
    /// (before the '+' metadata separator) is returned. Otherwise, returns the <see cref="AssemblyVersionAttribute"/> value.
    /// </summary>
    /// <returns>The version string from the entry assembly.</returns>
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
    /// Gets the commit SHA from the entry assembly's AssemblyInformationalVersion metadata.
    /// The entry assembly is the application using Chronicle, not Chronicle itself.
    /// Extracts the metadata portion after the '+' separator in the InformationalVersion attribute.
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
            if (plusIndex >= 0 && plusIndex < version.Length - 1)
            {
                return version[(plusIndex + 1)..];
            }
        }

        return "[N/A]";
    }
}
