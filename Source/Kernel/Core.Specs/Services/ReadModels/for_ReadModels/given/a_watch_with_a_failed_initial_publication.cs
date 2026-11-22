// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.given;

public class a_watch_with_a_failed_initial_publication : all_dependencies
{
    protected readonly Exception _publicationFailure = new TimeoutException("Watch publication failed");
    protected readonly List<ReadModelChangeset> _emitted = [];
    protected IProjectionChangesetNotifier _notifier;
    protected IReadModelChangesetSubscriber _subscriber;
    protected Guid _subscriptionId;
    protected Exception _result;

    void Establish()
    {
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(_notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(_subscriber);
        _notifier.Subscribe(_subscriber).Returns(Task.FromException(_publicationFailure));
        _changesetMediator.When(_ => _.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(call => _subscriptionId = call.Arg<Guid>());
    }

    protected async Task WatchUntilFaulted()
    {
        var faulted = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watch = _service.Watch(new WatchRequest
        {
            EventStore = "test-store",
            Namespace = "test-namespace",
            ReadModelIdentifier = "test-read-model"
        }).Subscribe(_emitted.Add, error => faulted.TrySetResult(error));
        _result = await faulted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
