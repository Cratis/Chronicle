// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Traces;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_observing_without_a_silo_host : Specification
{
    readonly Subject<ReactorMessage> _messages = new();
    readonly RecordingLogger<Reactors> _logger = new();
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Reactors _reactors;
    IDisposable _subscription;

    void Establish()
    {
        var mediator = Substitute.For<IReactorMediator>();
        mediator.When(_ => _.Subscribe(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReactorEventsObserver>()))
            .Do(_ => _subscribed.TrySetResult());
        _reactors = new Reactors(
            Substitute.For<IGrainFactory>(),
            null,
            mediator,
            Substitute.For<Cratis.Chronicle.Storage.IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            _logger);
    }

    async Task Because()
    {
        _subscription = _reactors.Observe(_messages).Subscribe(_ => { });
        _messages.OnNext(new(new(new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_describe_the_host_as_unavailable() => _logger.Messages.Exists(_ => _.Contains("unavailable: hostless in-process harness", StringComparison.Ordinal)).ShouldBeTrue();

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }
}
