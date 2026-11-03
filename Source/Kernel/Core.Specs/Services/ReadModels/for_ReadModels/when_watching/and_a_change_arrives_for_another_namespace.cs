// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_a_change_arrives_for_another_namespace : given.all_dependencies
{
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;
    readonly List<ReadModelChangeset> _emitted = [];

    void Establish()
    {
        _forwarderCaptured = new();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(Substitute.For<IProjectionChangesetNotifier>());
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(Substitute.For<IReadModelChangesetSubscriber>());
        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(ci => _forwarderCaptured.SetResult(ci.Arg<ChangesetForwarder>()));
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", Namespace = "namespace-a", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await forwarder(
            "namespace-b",
            "key-1",
            new JsonObject { ["name"] = "the-name" },
            new Concepts.ReadModels.ReadModelChangeContext(
                Concepts.ReadModels.ReadModelChangeType.Added,
                (Concepts.Events.EventSequenceNumber)5UL,
                DateTimeOffset.UtcNow,
                CorrelationId.NotSet));
    }

    [Fact] void should_not_emit_the_change() => _emitted.Count(_ => !_.Subscribed).ShouldEqual(0);
    [Fact] void should_not_release_anything() => _complianceHelper.ReceivedCalls().ShouldBeEmpty();
}
