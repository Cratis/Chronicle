// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyFenceProtocol.given;

/// <summary>
/// A started fence protocol whose heartbeat publication hangs on storage.
/// </summary>
public class a_started_protocol_with_a_hanging_publish : Specification
{
    protected readonly CancellationTokenSource _stopToken = new();
    protected readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _publishing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected EncryptionKeyFenceProtocol _protocol;
    ISiloLifecycle _lifecycle;

    async Task Establish()
    {
        var authority = Substitute.For<IEncryptionKeyFence, IEncryptionKeyFenceRecords>();
        var calls = 0;
        ((IEncryptionKeyFenceRecords)authority)
            .WriteRecord(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref calls) == 1) return Task.CompletedTask;
                _publishing.TrySetResult();
                return new TaskCompletionSource().Task;
            });
        var silo = Substitute.For<ILocalSiloDetails>();
        silo.SiloAddress.Returns(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([]);
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.Token.Returns(_deadline.Token);
        _protocol = new(
            storage,
            Substitute.For<IEncryptionKeyStorage>(),
            authority,
            silo,
            Options.Create(new ClusterOptions { ClusterId = "cluster" }),
            Options.Create(new ClusterMembershipOptions { IAmAliveTablePublishTimeout = TimeSpan.FromMilliseconds(10) }),
            Options.Create(new ChronicleOptions()),
            NullLogger<EncryptionKeyFenceProtocol>.Instance,
            TimeProvider.System,
            budget);
        ILifecycleObserver? observer = null;
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>())
            .Returns(call =>
            {
                observer = call.ArgAt<ILifecycleObserver>(2);
                return Substitute.For<IDisposable>();
            });
        _protocol.Participate(_lifecycle);
        await observer!.OnStart(CancellationToken.None);
        await _publishing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _observer = observer!;
    }

    protected ILifecycleObserver _observer = null!;

    void Destroy()
    {
        _protocol.Dispose();
        _stopToken.Dispose();
        _deadline.Dispose();
    }
}
