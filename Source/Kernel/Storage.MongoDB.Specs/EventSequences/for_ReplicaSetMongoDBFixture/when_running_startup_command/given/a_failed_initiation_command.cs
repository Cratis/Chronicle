// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_running_startup_command.given;

public static class a_failed_initiation_command
{
    public static async Task<(int ExitCode, string Stderr)> Run()
    {
        // Exercise the actual /bin/sh command without starting mongod or requiring Docker.
        const string mongod = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & ";
        var command = "mongosh() { if [ \"$1\" = \"--quiet\" ]; then return 0; fi; echo 'simulated initiation error' >&2; return 23; }; " +
            ReplicaSetMongoDBFixture.StartupCommand.Replace(mongod, string.Empty, StringComparison.Ordinal);
        var start = new ProcessStartInfo("/bin/sh") { RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(command);
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await process.StandardError.ReadToEndAsync());
    }
}
