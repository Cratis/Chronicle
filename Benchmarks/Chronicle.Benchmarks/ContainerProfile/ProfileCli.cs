// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The opt-in entry point; BenchmarkDotNet remains the default path.</summary>
internal static class ProfileCli
{
    /// <summary>Dispatches the profiling subcommand with meaningful exit codes.</summary>
    /// <param name="arguments">The subcommand arguments.</param>
    /// <returns>The command exit code.</returns>
    public static async Task<int> Run(string[] arguments)
    {
        var (options, error) = ProfileOptions.Parse(arguments);
        if (options is null)
        {
            await Console.Error.WriteLineAsync(error);
            return 2;
        }
        if (options.Command == "plan")
        {
            return ProfilePlan.Print(options);
        }
        using var cancellation = new CancellationTokenSource();
        void Handler(object? sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            cancellation.Cancel();
        }
        Console.CancelKeyPress += Handler;
        try
        {
            return await Execute(options, cancellation.Token);
        }
        catch (HarnessFailure failure)
        {
            await Console.Error.WriteLineAsync(failure.Message);
            return 1;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            await Console.Error.WriteLineAsync($"Harness failed: {exception.GetType().Name}. Child output withheld to protect credentials.");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= Handler;
        }
    }

    static async Task<int> Execute(ProfileOptions options, CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case "run":
                return await ProfileSweep.Run(options, cancellationToken);
            case "r2r-check":
                return await ProfileImages.Check(options, cancellationToken);
            case "build-images":
                await ProfileImages.Build(options, cancellationToken);
                return 0;
            case "cleanup":
                await CleanupCommands.Run(options.Session, cancellationToken);
                return 0;
            default:
                return 2;
        }
    }
}
