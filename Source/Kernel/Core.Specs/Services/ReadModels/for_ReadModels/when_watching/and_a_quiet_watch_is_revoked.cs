// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_a_quiet_watch_is_revoked : given.a_database_with_provenance
{
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Exception> _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _removed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionChangesetNotifier _notifier;
    IReadModelChangesetSubscriber _subscriber;
    Exception _error;
    Guid _subscriptionId;
    int _payloads;

    void Establish()
    {
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(_notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(_subscriber);
        _changesetMediator.When(_ => _.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>())).Do(call => _subscriptionId = call.Arg<Guid>());
        _changesetMediator.When(_ => _.Unsubscribe(Arg.Any<Guid>())).Do(_ => _removed.TrySetResult());
    }

    async Task Because()
    {
        using var watch = _service.Watch(new WatchRequest { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = "test-read-model" })
            .Subscribe(
                change =>
                {
                    if (change.Subscribed) _subscribed.TrySetResult();
                    else _payloads++;
                },
                error => _ended.TrySetResult(error));
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _sinks.GetProvenance(_readModelDefinition).MarkQuarantined();
        _error = await _ended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _removed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_end_with_the_neutral_refusal() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_remove_the_notifier_subscriber() => _notifier.Received(1).Unsubscribe(_subscriber);
    [Fact] void should_remove_the_mediator_callback() => _changesetMediator.Received(1).Unsubscribe(_subscriptionId);
    [Fact] void should_not_publish_payloads_after_revocation() => _payloads.ShouldEqual(0);
}
