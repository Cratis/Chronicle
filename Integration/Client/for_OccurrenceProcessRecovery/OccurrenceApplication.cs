// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public static class OccurrenceApplication
{
    public const string HolderVerb = "occurrence-holder-child";
    public const string RecoveryVerb = "occurrence-recovery-child";

    public static async Task<int> Run(string[] args, bool hold)
    {
        if (args is not ["--connection", var connection, "--event-store", var store])
        {
            throw new ArgumentException("Expected --connection and --event-store for the occurrence child.", nameof(args));
        }
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(300));
        using var barrier = new PostCommitBarrier(hold);
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSingleton(barrier);
        builder.AddCratisChronicle(
            options =>
            {
                options.ConnectionString = new(connection);
                options.EventStore = store;
                options.ActivationScope = ActivationScope.PerProcess;
            },
            configure: chronicle => chronicle.ClientArtifactsProvider = new DefaultClientArtifactsProvider(new RecoveryDiscovery()));
        using var host = builder.Build();
        await host.StartAsync(deadline.Token).WaitAsync(TimeSpan.FromSeconds(90));
        var eventStore = await host.Services.GetRequiredService<IChronicleClient>().GetEventStore(store).WaitAsync(TimeSpan.FromSeconds(60));
        (await eventStore.WaitForRegistration().WaitAsync(TimeSpan.FromSeconds(60))).IsSuccess.ShouldBeTrue();
        await barrier.Report($"READY {Environment.ProcessId}", deadline.Token);
        try
        {
            while (await Console.In.ReadLineAsync(deadline.Token) is { } command)
            {
                switch (command)
                {
                    case "record":
                        var result = await eventStore.Occurrences.Record(new RecoveryWork("recovery"), Guid.NewGuid().ToString("N")).WaitAsync(TimeSpan.FromSeconds(30));
                        result.Status.ShouldEqual(OccurrenceRecordStatus.Recorded);
                        await barrier.Report($"RECORDED {result.Id.Value}", deadline.Token);
                        break;
                    case "probe":
                        await barrier.Report($"ALIVE {Environment.ProcessId} {barrier.IsHeld}", deadline.Token);
                        break;
                    case "release":
                        barrier.Release();
                        break;
                    case "stop":
                        return 0;
                    default:
                        throw new InvalidOperationException($"Unknown occurrence child command: {command}");
                }
            }
            return 0;
        }
        finally
        {
            barrier.Release();
            using var stopDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await host.StopAsync(stopDeadline.Token).WaitAsync(stopDeadline.Token);
        }
    }
}
