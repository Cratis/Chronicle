// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_change_is_an_addition : given.all_dependencies
{
    IProjectionChangesetNotifier _notifier;
    TaskCompletionSource<IProjectionChangesetObserver> _observerCaptured;
    readonly List<ReadModelChangeset> _emitted = [];

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
            {
                { (Concepts.ReadModels.ReadModelGeneration)1, new JsonSchema() }
            }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _observerCaptured = new();
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(_notifier);
        _grainFactory
            .CreateObjectReference<IProjectionChangesetObserver>(Arg.Any<IProjectionChangesetObserver>())
            .Returns(ci => ci.Arg<IProjectionChangesetObserver>());

        _notifier.When(n => n.Subscribe(Arg.Any<IProjectionChangesetObserver>()))
            .Do(ci => _observerCaptured.SetResult(ci.Arg<IProjectionChangesetObserver>()));
        _notifier.Subscribe(Arg.Any<IProjectionChangesetObserver>()).Returns(Task.CompletedTask);
        _notifier.Unsubscribe(Arg.Any<IProjectionChangesetObserver>()).Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var observer = await _observerCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await observer.OnChangeset(
            "test-namespace",
            "key-1",
            new JsonObject { ["name"] = "the-name" },
            new Concepts.ReadModels.ReadModelChangeContext(
                Concepts.ReadModels.ReadModelChangeType.Added,
                (Concepts.Events.EventSequenceNumber)5UL,
                DateTimeOffset.UtcNow,
                Cratis.Execution.CorrelationId.NotSet));
    }

    [Fact] void should_emit_a_changeset_with_added_change_type() => _emitted.Single(_ => !_.Subscribed).ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_emit_the_causing_event_sequence_number() => _emitted.Single(_ => !_.Subscribed).EventSequenceNumber.ShouldEqual(5UL);
    [Fact] void should_not_mark_the_changeset_as_removed() => _emitted.Single(_ => !_.Subscribed).Removed.ShouldBeFalse();
}
