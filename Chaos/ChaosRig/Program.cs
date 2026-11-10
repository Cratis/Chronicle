// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal static class Program
{
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
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    static async Task<int> Clean(string[] args, CancellationToken cancellation)
    {
        if (args.Length != 0 && (args.Length != 2 || args[0] != "--run-id")) throw new ArgumentException("Use cleanup [--run-id <id>].");
        await Cleanup.Run(args.Length == 0 ? null : args[1], cancellation);
        return 0;
    }
}
