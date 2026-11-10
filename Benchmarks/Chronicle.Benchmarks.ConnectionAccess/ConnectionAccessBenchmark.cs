// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Clients;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks.ConnectionAccess;

/// <summary>
/// Measures local checks and remote misses against real, turn-serialized reservation state.
/// </summary>
[MemoryDiagnoser]
[InProcess]
[WarmupCount(1)]
[IterationCount(3)]
public class ConnectionAccessBenchmark
{
    const string Id = "connection";
    static readonly ConnectionOwner _owner = new("issuer", "alice");
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    readonly ConnectionOwnership _local = new();
    readonly ConnectionOwnership _remote = new();
    readonly string[] _connections = Enumerable.Range(0, 1000).Select(index => $"connection-{index}").ToArray();
    IGrainFactory _factory = null!;

    /// <summary>Gets or sets simulated cluster latency in milliseconds.</summary>
    [Params(0, 1)]
    public int LatencyMilliseconds { get; set; }

    /// <summary>Gets or sets registrations for one connection during a reconnect storm.</summary>
    [Params(50, 500)]
    public int Registrations { get; set; }

    /// <summary>Prepares reservations and the receiving silo's empty local index.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _local.Claim(Id, _owner);
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        var membership = Substitute.For<IClusterMembershipService>();
        var snapshot = new ClusterMembershipSnapshot(
            ImmutableDictionary<SiloAddress, ClusterMember>.Empty.Add(silo, new(silo, SiloStatus.Active, "silo")), new(1));
        membership.CurrentSnapshot.Returns(_ =>
        {
            membership.ClearReceivedCalls();
            return snapshot;
        });
        var reservations = _connections.Append(Id).ToDictionary(id => id, _ => CreateReservation(membership, silo));
        _factory = Substitute.For<IGrainFactory>();
        _factory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
        {
            // Keep the substitute's invocation history bounded during BenchmarkDotNet iterations.
            _factory.ClearReceivedCalls();
            return reservations[call.ArgAt<string>(0)];
        });
    }

    /// <summary>The ownership lookup before this patch.</summary>
    /// <returns>Whether the local index grants access.</returns>
    [Benchmark(Baseline = true)]
    public bool OldLocalCheck() => _local.IsOwnedBy(Id, _owner);

    /// <summary>The completed, allocation-free ValueTask path.</summary>
    /// <returns>The access outcome.</returns>
    [Benchmark]
    public ValueTask<ConnectionAccessOutcome> LocalHit() => _local.Verify(_factory, Id, _owner, _timeout);

    /// <summary>A local miss confirmed by the reservation.</summary>
    /// <returns>The access outcome.</returns>
    [Benchmark]
    public ValueTask<ConnectionAccessOutcome> LocalMissGrainHit() => _remote.Verify(_factory, Id, _owner, _timeout);

    /// <summary>Concurrent registrations for the same connection identifier.</summary>
    /// <returns>The access outcomes of all registrations.</returns>
    [Benchmark]
    public Task<ConnectionAccessOutcome[]> RegistrationStorm() => Task.WhenAll(
        Enumerable.Range(0, Registrations).Select(_ => _remote.Verify(_factory, Id, _owner, _timeout).AsTask()));

    /// <summary>One keep-alive per second for one thousand remotely held connections.</summary>
    /// <returns>The access outcomes of the one thousand checks.</returns>
    [Benchmark]
    public Task<ConnectionAccessOutcome[]> KeepAliveOverhead() => Task.WhenAll(
        _connections.Select(id => _remote.Verify(_factory, id, _owner, _timeout).AsTask()));

    DelayedReservation CreateReservation(IClusterMembershipService membership, SiloAddress silo)
    {
        var reservation = new BenchmarkReservation(membership);
        reservation.Claim(_owner, silo, Guid.NewGuid(), TimeSpan.FromHours(1)).GetAwaiter().GetResult();
        return new(reservation, LatencyMilliseconds);
    }
}
