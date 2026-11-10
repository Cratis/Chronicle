// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Chaos;

internal static class WorkloadApplication
{
    public static async Task<int> Run(string[] args, CancellationToken cancellation)
    {
        if (args.Length != 5) throw new ArgumentException("Worker requires role, connection, store, ledger connection, and run id.");
        var role = args[0];
        using var host = WorkloadHost.Create(role, args[1], args[2], new SideLedger(args[3], args[4]));
        await host.StartAsync(cancellation);
        var store = await host.Services.GetRequiredService<IChronicleClient>().GetEventStore(args[2]).WaitAsync(TimeSpan.FromSeconds(60), cancellation);
        var registration = await store.WaitForRegistration().WaitAsync(TimeSpan.FromSeconds(90), cancellation);
        if (!registration.IsSuccess) throw new InvalidOperationException("Worker artifact registration failed.");
        Console.WriteLine("READY");
        try
        {
            if (role == "producer") await Producer.Run(store, new Ledger(args[3], args[4]), cancellation);
            else await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
        }
        finally
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await host.StopAsync(shutdown.Token);
        }
        return 0;
    }
}
