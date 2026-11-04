// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Projects only structurally attributable connection endpoints into diagnostics.
/// </summary>
internal static class ConnectionStringDiagnostics
{
    /// <summary>
    /// The opaque diagnostic replacement for untrusted input.
    /// </summary>
    internal const string Redacted = "REDACTED";

    static readonly HashSet<string> _authoritySettings = new(["host", "port", "servers", "scheme", "username", "password"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Discards untrusted settings rather than interpreting them as diagnostic text.
    /// </summary>
    /// <param name="value">The setting to discard.</param>
    /// <returns>The opaque replacement.</returns>
    internal static string Hide(string value)
    {
        _ = value;
        return Redacted;
    }

    /// <summary>
    /// Determines whether the original text contains only an attributable authority.
    /// </summary>
    /// <param name="input">The original connection text.</param>
    /// <returns>Whether endpoint projection is permitted.</returns>
    internal static bool HasUnambiguousStructure(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var schemeEnd = input.IndexOf("://", StringComparison.Ordinal);
        return schemeEnd >= 0 && IsKnownScheme(input[..schemeEnd]) &&
            input[(schemeEnd + 3)..].IndexOfAny(['/', '?', '#']) < 0;
    }

    /// <summary>
    /// Builds a diagnostic endpoint without altering the operational builder.
    /// </summary>
    /// <param name="source">The operational settings.</param>
    /// <param name="uncertainInput">Whether the source text had uncertain attribution.</param>
    /// <returns>A safe endpoint or the opaque replacement.</returns>
    internal static string Build(ChronicleConnectionStringBuilder source, bool uncertainInput)
    {
        try
        {
            return uncertainInput ? Redacted : Project(source);
        }
#pragma warning disable CA1031 // Diagnostics must fail closed without affecting the operational builder.
        catch
#pragma warning restore CA1031
        {
            return Redacted;
        }
    }

    static bool IsKnownScheme(string scheme) =>
        scheme.Equals("chronicle", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("chronicle+srv", StringComparison.OrdinalIgnoreCase);

    static string Project(ChronicleConnectionStringBuilder source)
    {
        if (!IsKnownScheme(source.Scheme) || source.Keys.Cast<string>().Any(key => !_authoritySettings.Contains(key)))
        {
            return Redacted;
        }

        var addresses = source.ServerAddresses;
        if (addresses.Any(address => Uri.CheckHostName(address.Host) == UriHostNameType.Unknown || address.Port is < 1 or > 65535))
        {
            return Redacted;
        }

        // No input option or credential is copied to the diagnostic builder.
        return new ChronicleConnectionStringBuilder
        {
            Scheme = source.Scheme.ToLowerInvariant(),
            ServerAddresses = addresses,
            Username = string.IsNullOrEmpty(source.Username) ? null : Redacted,
            Password = string.IsNullOrEmpty(source.Password) ? null : Redacted
        }.Build();
    }
}
