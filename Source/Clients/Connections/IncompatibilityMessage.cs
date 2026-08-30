// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Formats what a client should say when it finds it cannot talk to a server.
/// </summary>
/// <remarks>
/// A refusal is only useful if it names what to change. Three versions answer that: the client SDK, the protocol
/// it speaks - which is the contracts version, and the unit compatibility is guaranteed in - and what the server
/// is running.
/// </remarks>
internal static class IncompatibilityMessage
{
    /// <summary>
    /// Builds the message for a server that no longer serves what this client expects.
    /// </summary>
    /// <param name="serverAddress">The address of the server that was contacted.</param>
    /// <param name="serverVersion">The version the server reported, if it reported one.</param>
    /// <param name="serverProtocolVersion">The protocol version the server reported, if it reported one.</param>
    /// <param name="incompatibilities">What the server said it no longer serves.</param>
    /// <returns>The message to carry on the exception and into the log.</returns>
    public static string Build(
        string serverAddress,
        string serverVersion,
        string serverProtocolVersion,
        IEnumerable<string> incompatibilities)
    {
        var listed = incompatibilities.ToList();
        var builder = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"The Chronicle server at {serverAddress} does not serve the contracts this client expects.")
            .AppendLine()
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"  Client:   {ChronicleClientIdentity.Version} ({ChronicleClientIdentity.Type})")
            .AppendLine(CultureInfo.InvariantCulture, $"  Protocol: {ChronicleClientIdentity.ProtocolVersion}")
            .AppendLine(CultureInfo.InvariantCulture, $"  Server:   {Describe(serverVersion)} (protocol {Describe(serverProtocolVersion)})");

        if (listed.Count > 0)
        {
            builder
                .AppendLine()
                .AppendLine(listed.Count == 1
                    ? "The server no longer serves 1 thing this client expects:"
                    : $"The server no longer serves {listed.Count.ToString(CultureInfo.InvariantCulture)} things this client expects:");

            listed.ForEach(_ => builder.AppendLine(CultureInfo.InvariantCulture, $"  - {_}"));
        }

        return builder.AppendLine().Append(Advise(serverProtocolVersion)).ToString();
    }

    static string Advise(string serverProtocolVersion)
    {
        var clientMajor = MajorOf(ChronicleClientIdentity.ProtocolVersion);
        var serverMajor = MajorOf(serverProtocolVersion);

        if (clientMajor is null || serverMajor is null || clientMajor == serverMajor)
        {
            // Within one protocol major this should not happen - it is what the release gate exists to prevent -
            // so say so rather than sending someone off to upgrade something that is already the right version.
            return "Client and server report the same protocol major, so this is a Chronicle defect rather than a version mismatch. Please report it with the list above.";
        }

        return clientMajor < serverMajor
            ? $"The server speaks protocol {serverMajor}, this client speaks {clientMajor}. Upgrade the client to a {serverMajor}.x release."
            : $"This client speaks protocol {clientMajor}, the server speaks {serverMajor}. Upgrade the server to a {clientMajor}.x release, or pin the client back to {serverMajor}.x.";
    }

    static int? MajorOf(string version)
    {
        var separator = version.IndexOf('.', StringComparison.Ordinal);
        var major = separator > 0 ? version[..separator] : version;
        return int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    static string Describe(string version) => string.IsNullOrEmpty(version) ? "unknown" : version;
}
