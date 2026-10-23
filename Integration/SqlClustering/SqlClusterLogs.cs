// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration.SqlClustering;

internal sealed class SqlClusterLogs
{
    readonly List<string> _replaced = [];

    public async Task CaptureReplaced(IContainer node) => _replaced.Add($"Replaced {await Read(node)}");

    public async Task Dump(IEnumerable<IContainer> nodes, TextWriter output)
    {
        foreach (var logs in _replaced)
        {
            await output.WriteLineAsync(logs);
        }

        foreach (var node in nodes)
        {
            await output.WriteLineAsync(await Read(node));
        }
    }

    static async Task<string> Read(IContainer node)
    {
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var logs = await node.GetLogsAsync(ct: deadline.Token).WaitAsync(deadline.Token);
            return $"{node.Name} STDOUT:\n{logs.Stdout}\nSTDERR:\n{logs.Stderr}";
        }
        catch (Exception exception)
        {
            return $"{node.Name} logs unavailable: {exception}";
        }
    }
}
