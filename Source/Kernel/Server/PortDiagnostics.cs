// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Names whatever is listening on a TCP port inside this container, read directly from the
/// process filesystem's TCP tables and per-process file descriptors rather than shelling out to a
/// packaged tool such as ss, lsof or netstat - none of them is guaranteed to be installed in a
/// Chronicle image, and a diagnostic that needs a package installed before it can report is a
/// diagnostic that is missing on the day it is needed.
/// </summary>
/// <remarks>
/// The Kernel intermittently aborts at startup with "Failed to bind to address ...: address already in
/// use", the container exits, and every test sharing that fixture fails. The pre-flight and post-mortem
/// checks around the container entrypoint (report-port-holder.sh, #4048) both miss the holder because
/// it has already released the port by the time either of them runs - only a snapshot taken at the
/// moment the bind itself fails can name it. See #4174.
/// </remarks>
internal static class PortDiagnostics
{
    /// <summary>
    /// Describes every listening socket currently bound to the given port, and the process holding it
    /// when one can be identified.
    /// </summary>
    /// <param name="port">The port to inspect.</param>
    /// <returns>
    /// One line per listening socket found, or a single line saying none were found. Empty when the
    /// diagnostic cannot run at all on this platform or in this sandbox.
    /// </returns>
    internal static IReadOnlyList<string> DescribeListeners(int port)
    {
        if (!OperatingSystem.IsLinux())
        {
            return [];
        }

        try
        {
            var inodes = FindListeningInodes(port);
            return inodes.Count == 0
                ? [$"port {port} is not listening inside this container at the moment of failure."]
                : inodes.Select(inode => DescribeHolder(port, inode)).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // /proc is not guaranteed to be readable in every sandbox this runs in. A diagnostic that
            // cannot read /proc must not throw on top of the bind failure it exists to explain.
            return [$"port diagnostics for port {port} are unavailable: {exception.Message}"];
        }
    }

    static List<string> FindListeningInodes(int port)
    {
        var hexPort = port.ToString("X4");
        var inodes = new List<string>();

        foreach (var path in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 10)
                {
                    continue;
                }

                // 0A is TCP_LISTEN. Anything else is a connection, not something that blocks a bind.
                if (fields[3] != "0A")
                {
                    continue;
                }

                var localPort = fields[1][(fields[1].IndexOf(':') + 1)..];
                if (string.Equals(localPort, hexPort, StringComparison.OrdinalIgnoreCase))
                {
                    inodes.Add(fields[9]);
                }
            }
        }

        return inodes;
    }

    static string DescribeHolder(int port, string inode)
    {
        var target = $"socket:[{inode}]";

        foreach (var processDirectory in Directory.EnumerateDirectories("/proc"))
        {
            var pid = Path.GetFileName(processDirectory);
            if (!int.TryParse(pid, out _))
            {
                continue;
            }

            if (!TryFindMatchingDescriptor(processDirectory, target))
            {
                continue;
            }

            var command = ReadFirstLine(Path.Combine(processDirectory, "comm")) ?? "unknown";
            var arguments = ReadFirstLine(Path.Combine(processDirectory, "cmdline"))?.Replace('\0', ' ').Trim();
            return $"port {port} is held by pid {pid} ({command}): {(string.IsNullOrEmpty(arguments) ? "no command line" : arguments)}";
        }

        // A socket with no owning file descriptor in this namespace - most often a process that has
        // already exited leaving the socket in TIME_WAIT, or one owned by another container sharing
        // this network namespace. Worth saying out loud rather than reporting nothing.
        return $"port {port} is held by socket inode {inode}, which no visible process owns";
    }

    static bool TryFindMatchingDescriptor(string processDirectory, string target)
    {
        var fdPath = Path.Combine(processDirectory, "fd");

        IEnumerable<string> descriptors;
        try
        {
            descriptors = Directory.EnumerateFiles(fdPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The process can exit, or be another container's, between the outer listing and this read.
            return false;
        }

        foreach (var descriptor in descriptors)
        {
            string? link;
            try
            {
                link = new FileInfo(descriptor).LinkTarget;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (link == target)
            {
                return true;
            }
        }

        return false;
    }

    static string? ReadFirstLine(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
