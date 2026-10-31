// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias baseline;

using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using CachedRoster = baseline::Cratis.Chronicle.Security.AdministratorRoster;
using CurrentRoster = Cratis.Chronicle.Security.AdministratorRoster;
using RosterSource = Cratis.Chronicle.Concepts.Security.AdministratorRoster;
using StoredUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Benchmarks.Roster;

/// <summary>
/// Compares the warmed a5e06dbf0e cache with verified roster replay using identical storage and signed slots.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[InProcess]
[WarmupCount(3)]
[IterationCount(10)]
[InvocationCount(256)]
public class AdministratorRosterBenchmark
{
    readonly UserId _user = Guid.Parse("613e481b-faae-469b-9e99-86248f728a20");
    CachedRoster _cached = null!;
    CurrentRoster _replay = null!;
    IStorage _storage = null!;
    IEventSequenceStorage _events = null!;
    IEventSerializer _serializer = null!;

    /// <summary>
    /// Gets or sets the number of signed roster slots.
    /// </summary>
    [Params(5, 20, 100)]
    public int Slots { get; set; }

    /// <summary>
    /// Constructs shared in-memory storage and warms both roster instances.
    /// </summary>
    /// <returns>The setup task.</returns>
    /// <exception cref="InvalidOperationException">One of the rosters did not accept the signed benchmark history.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _storage = Substitute.For<IStorage>();
        _events = Substitute.For<IEventSequenceStorage>();
        _serializer = Substitute.For<IEventSerializer>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default)
            .GetEventSequence(EventSequenceId.Log).Returns(_events);
        var user = new StoredUser { Id = _user, Username = "root", IsActive = true };
        _storage.System.Users.GetById(_user).Returns(user);
        var proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        var history = BuildHistory(proof);
        _events.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>()).Returns((EventSequenceNumber)(ulong)(Slots - 1));
        _events.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(_ => Cursor(history));
        var factory = Substitute.For<IGrainFactory>();
        var configuration = new ConfigurationBuilder().Build();
        var options = Options.Create(new ChronicleOptions());
        _cached = new(_storage, factory, _serializer, options, configuration, NullLogger<CachedRoster>.Instance);
        _replay = new(_storage, factory, _serializer, proof, options, configuration, NullLogger<CurrentRoster>.Instance);
        if (!await _cached.IsActiveAdministrator(_user) || !await _replay.IsActiveAdministrator(_user))
        {
            throw new InvalidOperationException("Both roster implementations must grant the benchmark user.");
        }
    }

    /// <summary>
    /// Clears substitute call recording between iterations without changing the warmed roster cache.
    /// </summary>
    [IterationCleanup]
    public void ClearCalls()
    {
        _events.ClearReceivedCalls();
        _serializer.ClearReceivedCalls();
        _storage.System.Users.ClearReceivedCalls();
    }

    /// <summary>
    /// Checks the warmed original roster from a5e06dbf0e.
    /// </summary>
    /// <returns>The current authorization result.</returns>
    [Benchmark(Baseline = true)]
    public Task<bool> Cached() => _cached.IsActiveAdministrator(_user);

    /// <summary>
    /// Checks the current roster, including proof verification and replay.
    /// </summary>
    /// <returns>The current authorization result.</returns>
    [Benchmark]
    public Task<bool> Replay() => _replay.IsActiveAdministrator(_user);

    static IEventCursor Cursor(IEnumerable<AppendedEvent> history)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(history);
        return cursor;
    }

    AppendedEvent[] BuildHistory(KernelEventProof proof)
    {
        var history = new List<AppendedEvent>();
        for (var index = 0; index < Slots; index++)
        {
            var type = typeof(UserGrantedAdministrator).GetEventType();
            var predecessor = index == 0 ? EventSequenceNumber.BeforeFirst : (EventSequenceNumber)(ulong)(index - 1);
            var body = JsonSerializer.Serialize(new[] { _user.ToString(), ((int)GrantReason.AdministratorGrant).ToString(CultureInfo.InvariantCulture), "benchmark" });
            var content = new UserGrantedAdministrator(_user, GrantReason.AdministratorGrant, "benchmark", proof.Sign(type, RosterSource.EventSourceId, predecessor, body));
            var appended = AppendedEvent.Empty() with
            {
                Context = EventContext.Empty with { EventType = type, EventSourceId = RosterSource.EventSourceId, SequenceNumber = (ulong)index }
            };
            _serializer.Deserialize(appended).Returns(content);
            history.Add(appended);
        }
        return [.. history];
    }
}
