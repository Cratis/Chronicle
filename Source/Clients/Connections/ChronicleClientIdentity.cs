// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the identity of this Chronicle client SDK - what it is, what version it is, and what protocol it speaks.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="ClientProcess"/> on purpose: that describes the application that happens to be hosting
/// the client, while this describes the client itself. When a connection is refused, these are the values that say
/// which piece of software has to change.
/// </remarks>
internal static class ChronicleClientIdentity
{
    /// <summary>
    /// Gets the type of the client - always <c>.NET</c> for this SDK, so the server and its Workbench can tell it
    /// apart from clients built on the other Chronicle SDKs.
    /// </summary>
    public static string Type => ".NET";

    /// <summary>
    /// Gets the version of this client SDK.
    /// </summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>
    /// Gets the version of the contracts this client was built against - the protocol it speaks.
    /// </summary>
    public static string ProtocolVersion => Contracts.ProtocolVersion.Current;

    static string ReadVersion()
    {
        var assembly = typeof(ChronicleClientIdentity).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        var plusIndex = informational.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? informational[..plusIndex] : informational;
    }
}
