// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

public static class PerProcessApplication
{
    public const string Verb = "for-per-process-child";

    public static async Task<int> Run(string[] args)
    {
        if (args is not ["--connection", var connection, "--event-store", var store])
        {
            throw new ArgumentException("Expected --connection and --event-store for the child application.", nameof(args));
        }
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.AddCratisChronicle(
            options =>
            {
                options.ConnectionString = new(connection);
                options.EventStore = store;
                options.ActivationScope = ActivationScope.PerProcess;
            },
            configure: chronicle => chronicle.ClientArtifactsProvider = new DefaultClientArtifactsProvider(new PerProcessDiscovery()));
        using var host = builder.Build();
        await host.StartAsync().WaitAsync(TimeSpan.FromSeconds(90));
        await Console.Out.WriteLineAsync($"READY {Environment.ProcessId}");
        await Console.Out.FlushAsync();
        var exitCode = await WaitForStop();
        using var stopDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await host.StopAsync(stopDeadline.Token);
        return exitCode;
    }

    static async Task<int> WaitForStop()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(240));
        try
        {
            while (await Console.In.ReadLineAsync(deadline.Token) is { } line)
            {
                if (line == "stop") return 0;
            }
            return 0;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return 3;
        }
    }
}
