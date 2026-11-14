// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_namespace_casing_differs;

public class and_reading_observer_state : for_Observers.given.a_case_sensitive_observer_store
{
    [Theory]
    [InlineData("cratis")]
    [InlineData("CRATIS")]
    [InlineData("Cratis")]
    public async Task should_read_the_canonical_observer_list(string @namespace)
    {
        var result = (await _service.GetObservers(new AllObserversRequest { EventStore = "store", Namespace = @namespace })).Single();
        result.Id.ShouldEqual("observer");
        result.NextEventSequenceNumber.ShouldEqual(42UL);
        ((DateTimeOffset?)result.RunningStateChangedAt).ShouldEqual(_changedAt);
    }

    [Theory]
    [InlineData("cratis")]
    [InlineData("CRATIS")]
    [InlineData("Cratis")]
    public async Task should_read_replayable_observers_from_the_canonical_namespace(string @namespace)
    {
        var result = (await _service.GetReplayableObserversForEventTypes(new GetReplayableObserversForEventTypesRequest
        {
            EventStore = "store",
            Namespace = @namespace,
            EventTypes = [_eventType.ToContract()]
        })).Single();
        result.Id.ShouldEqual("observer");
        result.NextEventSequenceNumber.ShouldEqual(42UL);
        ((DateTimeOffset?)result.RunningStateChangedAt).ShouldEqual(_changedAt);
    }

    [Theory]
    [InlineData("cratis")]
    [InlineData("CRATIS")]
    [InlineData("Cratis")]
    public async Task should_read_information_and_timestamp_from_the_same_namespace(string @namespace)
    {
        var result = await _service.GetObserverInformation(new GetObserverInformationRequest
        {
            EventStore = "store",
            Namespace = @namespace,
            ObserverId = "observer",
            EventSequenceId = "event-log"
        });
        result.Id.ShouldEqual("observer");
        ((DateTimeOffset?)result.RunningStateChangedAt).ShouldEqual(_changedAt);
        _grains.Received(1).GetGrain<global::Cratis.Chronicle.Observation.IObserver>("observer#store#Cratis#event-log");
    }

    [Theory]
    [InlineData("cratis")]
    [InlineData("CRATIS")]
    [InlineData("Cratis")]
    public async Task should_observe_the_canonical_namespace(string @namespace)
    {
        var result = (await _service.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = @namespace })
            .Timeout(TimeSpan.FromSeconds(5)).FirstAsync()).Single();
        result.Id.ShouldEqual("observer");
        result.NextEventSequenceNumber.ShouldEqual(42UL);
        ((DateTimeOffset?)result.RunningStateChangedAt).ShouldEqual(_changedAt);
    }

    [Fact]
    public async Task should_not_find_the_canonical_state_without_casing_resolution()
    {
        var identity = CreateService(NamespaceCasingContext.Create());
        var observers = await identity.GetObservers(new AllObserversRequest { EventStore = "store", Namespace = "cratis" });
        observers.Single().NextEventSequenceNumber.ShouldNotEqual(42UL);
        var replayable = await identity.GetReplayableObserversForEventTypes(new GetReplayableObserversForEventTypesRequest
        {
            EventStore = "store",
            Namespace = "cratis",
            EventTypes = [_eventType.ToContract()]
        });
        replayable.ShouldBeEmpty();
        var observed = await identity.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = "cratis" })
            .Timeout(TimeSpan.FromSeconds(5)).FirstAsync();
        observed.Single().NextEventSequenceNumber.ShouldNotEqual(42UL);
    }

    [Fact]
    public async Task should_preserve_other_case_variant_subscribers_when_one_call_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        var cancelledCompleted = false;
        var remainingCompleted = false;
        var remainingEmissions = new List<ObserverInformation[]>();
        using var cancelled = _service.ObserveObservers(
            new AllObserversRequest { EventStore = "store", Namespace = "cratis" },
            new CallContext(new CallOptions(cancellationToken: cancellation.Token)))
            .Subscribe(_ => { }, () => cancelledCompleted = true);
        using var remaining = _service.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = "CRATIS" })
            .Subscribe(observers => remainingEmissions.Add(observers.ToArray()), () => remainingCompleted = true);

        await cancellation.CancelAsync();
        await _canonicalStorage.Observers.Save(_state with { NextEventSequenceNumber = 43 }, EventSequenceId.Log);

        cancelledCompleted.ShouldBeTrue();
        remainingCompleted.ShouldBeFalse();
        remainingEmissions[^1].Single().NextEventSequenceNumber.ShouldEqual(43UL);
    }

    [Fact]
    public async Task should_complete_a_cancelled_call_even_while_resolution_is_pending()
    {
        var pending = new TaskCompletionSource<EventStoreNamespaceName>(TaskCreationOptions.RunContinuationsAsynchronously);
        var casing = NamespaceCasingContext.Create();
#pragma warning disable CA2012 // NSubstitute configures the returned ValueTask; it is not consumed here.
        casing.Canonicalize("store", "cratis").Returns(_ => new ValueTask<EventStoreNamespaceName>(pending.Task));
#pragma warning restore CA2012
        using var cancellation = new CancellationTokenSource();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = CreateService(casing).ObserveObservers(
            new AllObserversRequest { EventStore = "store", Namespace = "cratis" },
            new CallContext(new CallOptions(cancellationToken: cancellation.Token)))
            .Subscribe(_ => { }, completed.SetResult);

        await cancellation.CancelAsync();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), _service.CompletionTimeProvider);
        pending.SetResult("Cratis");
    }

    [Theory]
    [InlineData("System", "cratis")]
    [InlineData("store", "cratis#other")]
    public async Task should_refuse_invalid_or_unauthorized_reads_before_resolving_the_namespace(string store, string @namespace)
    {
        _grains.ClearReceivedCalls();
        var context = CallFor(new Probe(), CallerNamed(OrdinaryUser));
        await Assert.ThrowsAsync<RpcException>(() => _service.GetObservers(new AllObserversRequest { EventStore = store, Namespace = @namespace }, context));
        await Assert.ThrowsAsync<RpcException>(async () => await _service.ObserveObservers(new AllObserversRequest { EventStore = store, Namespace = @namespace }, context).FirstAsync());
        _grains.ReceivedCalls().ShouldBeEmpty();
    }
}
