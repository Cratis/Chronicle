// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Describes Linux TCP sockets using a port, including outbound source ports (not just listeners).
/// Diagnostics must never prevent the server from binding on hosts without /proc.
/// </summary>
internal static class TcpPortHolders
{
    static readonly string[] _tcpFiles = ["/proc/net/tcp", "/proc/net/tcp6"];

    /// <summary>
    /// Describes the sockets using an endpoint's local port at the instant of a bind conflict.
    /// </summary>
    /// <param name="endpoint">The endpoint that failed to bind.</param>
    /// <returns>The state and owner of every socket using the port, if available.</returns>
    internal static string Describe(EndPoint endpoint)
    {
        if (endpoint is not IPEndPoint ipEndpoint || !OperatingSystem.IsLinux())
        {
            return "port diagnostics unavailable";
        }

        try
        {
            var sockets = _tcpFiles
                .Where(File.Exists)
                .SelectMany(File.ReadLines)
                .Select(line => Parse(line, ipEndpoint.Port))
                .Where(socket => socket is not null)
                .ToArray();

            return sockets.Length == 0
                ? "no socket at diagnostic snapshot"
                : string.Join("; ", sockets.Select(socket => $"state {socket!.Value.State}, remote {socket.Value.Remote}, inode {socket.Value.Inode}, owner {Owner(socket.Value.Inode)}"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "port diagnostics unavailable";
        }
    }

    /// <summary>
    /// Parses a procfs TCP socket row matching the requested local port, regardless of state.
    /// </summary>
    /// <param name="line">The procfs socket table row.</param>
    /// <param name="port">The requested local port.</param>
    /// <returns>The matching socket, or null if this row does not use the port.</returns>
    internal static (string State, string Remote, string Inode)? Parse(string line, int port)
    {
        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 10 || !fields[1].Contains(':'))
        {
            return null;
        }

        var hexPort = fields[1][(fields[1].LastIndexOf(':') + 1)..];
        if (!int.TryParse(hexPort, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var localPort) || localPort != port)
        {
            return null;
        }

        return (fields[3], fields[2], fields[9]);
    }

    static string Owner(string inode)
    {
        foreach (var directory in Directory.EnumerateDirectories("/proc"))
        {
            var pid = Path.GetFileName(directory);
            if (!int.TryParse(pid, out _) || !Directory.Exists(Path.Combine(directory, "fd")))
            {
                continue;
            }

            try
            {
                foreach (var fd in Directory.EnumerateFiles(Path.Combine(directory, "fd")))
                {
                    if (new FileInfo(fd).LinkTarget == $"socket:[{inode}]")
                    {
                        return $"pid {pid} ({File.ReadAllText(Path.Combine(directory, "comm")).Trim()})";
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A process can disappear, or its descriptors can be inaccessible.
            }
        }

        return "not visible";
    }
}
