// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Collects best-effort diagnostics from an out-of-process MongoDB container that failed to start.
/// </summary>
public static partial class MongoContainerStartupDiagnostics
{
    const int MaxLogLines = 100;
    const int MaxOutputBytes = 8192;
    static readonly TimeSpan _stepTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Collect diagnostics for the container. Every step is bounded and failures of a step are reported, never thrown.
    /// </summary>
    /// <param name="container">The container that failed to start.</param>
    /// <returns>A bounded description of the container state, recent output and replica set status.</returns>
    public static string Collect(IContainer container)
    {
        var builder = new StringBuilder();
        Step(builder, "container state", () => Task.FromResult(container.State.ToString()));
        Step(builder, "recent output", () => ReadLogs(container));
        Step(builder, "rs.status()", async () =>
        {
            using var cts = new CancellationTokenSource(_stepTimeout);

            // Cap the merged streams inside the container, before ExecAsync buffers them on the host.
            var result = await container.ExecAsync(
                ["/bin/sh", "-c", $"mongosh --quiet --eval 'JSON.stringify(rs.status())' 2>&1 | head -c {MaxOutputBytes}"], cts.Token);
            return $"bounded shell exit {result.ExitCode}\n{result.Stdout}\n{result.Stderr}";
        });
        return builder.ToString();
    }

    /// <summary>
    /// Attempt bounded disposal and report failure without masking the original startup exception.
    /// </summary>
    /// <param name="container">The container to dispose.</param>
    /// <returns>The disposal outcome.</returns>
    public static string Dispose(IContainer container)
    {
        var builder = new StringBuilder();
        Step(builder, "container disposal", async () =>
        {
            await container.DisposeAsync();
            return "completed";
        });
        return builder.ToString();
    }

    [GeneratedRegex(@"password|passwd|pwd|credential|secret|token|authorization|authentication|api.?key|private.?key|mongodb(?:\+srv)?://|https?://", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SensitiveLine { get; }

    static async Task<string> ReadLogs(IContainer container)
    {
        using var cts = new CancellationTokenSource(_stepTimeout);
        using var client = TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();
        using var stream = await client.Containers.GetContainerLogsAsync(
            container.Id,
            new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false,
                Tail = MaxLogLines.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            cts.Token);
        var bytes = new byte[MaxOutputBytes];
        var count = 0;
        while (count < bytes.Length)
        {
            var read = await stream.ReadOutputAsync(bytes, count, bytes.Length - count, cts.Token);
            if (read.EOF)
            {
                break;
            }
            count += read.Count;
        }
        return $"STDOUT/STDERR (up to {MaxLogLines} recent lines, first {MaxOutputBytes} bytes):\n{Encoding.UTF8.GetString(bytes, 0, count)}";
    }

    static string Render(string text)
    {
        // Omit whole sensitive lines rather than guessing where a credential ends.
        var redacted = string.Join('\n', text.Split('\n').Select(line => SensitiveLine.IsMatch(line) ? "[sensitive diagnostic line omitted]" : line));
        var bytes = 0;
        var characters = 0;
        foreach (var rune in redacted.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > MaxOutputBytes)
            {
                break;
            }
            bytes += rune.Utf8SequenceLength;
            characters += rune.Utf16SequenceLength;
        }
        return redacted[..characters];
    }

    static void Step(StringBuilder builder, string name, Func<Task<string>> collect)
    {
        builder.AppendLine($"--- {name} ---");
        try
        {
            builder.AppendLine(Render(Task.Run(collect).WaitAsync(_stepTimeout).GetAwaiter().GetResult()));
        }
        catch (Exception ex)
        {
            // Exception messages can contain connection strings or daemon response bodies.
            builder.AppendLine($"(could not complete {name}: {ex.GetType().Name})");
        }
    }
}
