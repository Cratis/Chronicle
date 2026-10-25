// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

/// <summary>Reducer watches use the reducer's observer identifier.</summary>
public class and_the_read_model_is_reducer_backed : given.all_dependencies
{
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _cancellation = new();
    IProjectionChangesetNotifier _notifier;
    IDisposable _subscription;
    Exception _error;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with { ObserverType = Concepts.ReadModels.ReadModelObserverType.Reducer, ObserverIdentifier = "my-reducer" };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(_readModelDefinition.ObserverIdentifier).Returns(_notifier);
    }

    async Task Because()
    {
        _subscription = _service.Watch(new WatchRequest { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "event-log" }, new CallContext(new CallOptions(cancellationToken: _cancellation.Token)))
            .Subscribe(
                change =>
                {
                    if (change.Subscribed) _subscribed.TrySetResult();
                },
                error =>
                {
                    _error = error;
                    _subscribed.TrySetException(error);
                });
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_subscribe_the_notifier() => _notifier.Received(1).Subscribe(Arg.Any<IReadModelChangesetSubscriber>());
    [Fact] void should_not_report_an_error() => _error.ShouldBeNull();

    void Destroy()
    {
        _cancellation.Cancel();
        _subscription.Dispose();
        _cancellation.Dispose();
    }
}
