// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// A throwaway MongoDB replica set in Docker, one container per member on a private network, with every member published on a
/// host port so its server statistics can be read directly.
/// </summary>
/// <param name="members">The number of members; the first is the preferred primary.</param>
/// <param name="image">The MongoDB image to run.</param>
internal sealed class FenceCostReplicaSet(int members, string image)
{
    const int FirstHostPort = 37017;
    readonly string _network = $"fence-cost-{Guid.NewGuid():N}"[..20];

    /// <summary>
    /// Gets the connection string of the primary.
    /// </summary>
    internal string PrimaryConnectionString => MemberConnectionString(0);

    /// <summary>
    /// Gets the number of members.
    /// </summary>
    internal int Members => members;

    /// <summary>
    /// Gets the connection string of one member, connected to directly.
    /// </summary>
    /// <param name="member">The member index.</param>
    /// <returns>The connection string.</returns>
    internal static string MemberConnectionString(int member) => $"mongodb://localhost:{FirstHostPort + member}/?directConnection=true";

    /// <summary>
    /// Removes the members and their network.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task Stop()
    {
        for (var member = 0; member < members; member++)
        {
            await Docker($"rm -f {_network}-m{member}", throwOnError: false);
        }

        await Docker($"network rm {_network}", throwOnError: false);
    }

    /// <summary>
    /// Starts the members and initiates the replica set, waiting until the first member is primary.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task Start()
    {
        await Docker($"network create {_network}");
        for (var member = 0; member < members; member++)
        {
            await Docker($"run -d --name {_network}-m{member} --network {_network} --network-alias m{member} -p {FirstHostPort + member}:27017 {image} --replSet rs0 --bind_ip_all --oplogSize 20000");
        }

        var configuration = new BsonDocument
        {
            { "_id", "rs0" },
            { "members", new BsonArray(Enumerable.Range(0, members).Select(member => new BsonDocument { { "_id", member }, { "host", $"m{member}:27017" }, { "priority", member == 0 ? 2 : 1 } })) }
        };
        using var client = new MongoClient(PrimaryConnectionString);
        var admin = client.GetDatabase("admin");
        await Retry(() => admin.RunCommandAsync<BsonDocument>(new BsonDocument("replSetInitiate", configuration)));
        await Retry(async () =>
        {
            var hello = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
            if (!hello.GetValue("isWritablePrimary", false).AsBoolean || (members > 1 && hello["hosts"].AsBsonArray.Count != members))
            {
                throw new InvalidOperationException("The replica set has no primary yet.");
            }

            return hello;
        });
    }

    static async Task Retry<T>(Func<Task<T>> operation)
    {
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception) when (deadline.Elapsed < TimeSpan.FromSeconds(90))
            {
                await Task.Delay(500);
            }
        }
    }

    static async Task Docker(string arguments, bool throwOnError = true)
    {
        using var process = Process.Start(new ProcessStartInfo("docker", arguments) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker {arguments} failed: {error}");
        }
    }
}
