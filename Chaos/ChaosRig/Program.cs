// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Dispatches the chaos driver, workers, and cleanup command.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Executes a rig command.
    /// </summary>
    /// <param name="args">Command and options.</param>
    /// <returns>The command exit code.</returns>
    /// <exception cref="ArgumentException">The command verb is missing or unknown.</exception>
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        try
        {
            if (args.Length == 0) throw new ArgumentException("Use run or cleanup.");
            return args[0] switch
            {
                "run" => await ChaosRun.Run(RunOptionsParser.Parse(args[1..]), cancellation.Token),
                "producer" or "consumer" => await WorkloadApplication.Run(args, cancellation.Token),
                "cleanup" => await Clean(args[1..], cancellation.Token),
                _ => throw new ArgumentException($"Unknown verb: {args[0]}")
            };
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.ToString());
            return 1;
        }
    }

    static async Task<int> Clean(string[] args, CancellationToken cancellation)
    {
        await Cleanup.Run(CleanupRunId(args), cancellation);
        return 0;
    }

    static string? CleanupRunId(string[] args)
    {
        if (args.Length == 0) return null;
        if (args.Length != 2) throw new ArgumentException("Use cleanup [--run-id <id>].");
        if (args[0] != "--run-id") throw new ArgumentException("Use cleanup [--run-id <id>].");
        return args[1];
    }
}
