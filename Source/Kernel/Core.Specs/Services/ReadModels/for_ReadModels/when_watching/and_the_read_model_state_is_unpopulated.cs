// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_read_model_state_is_unpopulated : given.all_dependencies
{
    readonly TaskCompletionSource<ReadModelChangeset> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ReadModelChangeset _result;
    int _definitionReads;

    void Establish()
    {
        ((ReadModels)_service).ReadModelDefinitionRetryDelay = TimeSpan.Zero;
        var unpopulated = (Concepts.ReadModels.ReadModelDefinition)RuntimeHelpers.GetUninitializedObject(typeof(Concepts.ReadModels.ReadModelDefinition));
        _readModel.GetDefinition().Returns(_ => Task.FromResult(++_definitionReads == 1 ? unpopulated : _readModelDefinition));

        var notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(Substitute.For<IReadModelChangesetSubscriber>());
    }

    async Task Because()
    {
        _service.Watch(new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" })
            .Subscribe(change =>
            {
                if (change.Subscribed)
                {
                    _subscribed.TrySetResult(change);
                }
            },
            error => _subscribed.TrySetException(error));

        _result = await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_retry_until_the_definition_is_registered() => _definitionReads.ShouldBeGreaterThan(1);
    [Fact] void should_subscribe_to_the_registered_read_model() => _result.Subscribed.ShouldBeTrue();
}
