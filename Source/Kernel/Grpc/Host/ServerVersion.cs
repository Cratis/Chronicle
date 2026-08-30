// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Services.Host;

/// <summary>
/// Provides the version of the running server.
/// </summary>
/// <remarks>
/// The entry assembly is the Chronicle server executable, which the publish pipeline stamps with the actual release
/// version through <c>-p:Version=</c>. The Core assembly is a library dependency whose version stays at 1.0.0, so
/// reading it instead would report every release as 1.0.0.
/// </remarks>
internal static class ServerVersion
{
    static readonly string _informationalVersion = ReadInformationalVersion();

    /// <summary>
    /// Gets the version of the running server.
    /// </summary>
    public static string Version { get; } = ParseVersion(_informationalVersion);

    /// <summary>
    /// Gets the commit the running server was built from, or an empty string when it is not stamped.
    /// </summary>
    public static string CommitSha { get; } = ParseCommitSha(_informationalVersion);

    /// <summary>
    /// Parses the version portion from an assembly informational version string, dropping any build metadata.
    /// </summary>
    /// <param name="informationalVersion">The informational version string, for example <c>15.9.0+abc123</c>.</param>
    /// <returns>The version portion before any '+' separator, or the whole string when there is none.</returns>
    internal static string ParseVersion(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
    }

    /// <summary>
    /// Parses the commit from an assembly informational version string.
    /// </summary>
    /// <param name="informationalVersion">The informational version string, for example <c>15.9.0+abc123</c>.</param>
    /// <returns>The build metadata after the '+' separator, or an empty string when there is none.</returns>
    internal static string ParseCommitSha(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 && plusIndex < informationalVersion.Length - 1
            ? informationalVersion[(plusIndex + 1)..]
            : string.Empty;
    }

    static string ReadInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(ServerVersion).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
