// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Provides the version of the wire contracts - the protocol version a client or server speaks.
/// </summary>
/// <remarks>
/// This is the version of the contracts assembly, which the publish pipeline stamps with the release version. The
/// major is the compatibility unit: everything released within one major is verified against the first release of
/// that major before it ships, so two peers on the same protocol major are known to understand each other.
/// </remarks>
public static class ProtocolVersion
{
    /// <summary>
    /// Gets the version of the contracts this assembly carries.
    /// </summary>
    public static string Current { get; } = Read();

    static string Read()
    {
        var assembly = typeof(ProtocolVersion).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        // Build metadata after '+' is the commit sha, which says nothing about the contract.
        var plusIndex = informational.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? informational[..plusIndex] : informational;
    }
}
