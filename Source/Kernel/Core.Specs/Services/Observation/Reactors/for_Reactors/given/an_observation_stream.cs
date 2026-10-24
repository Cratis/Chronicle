// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class an_observation_stream : Specification
{
    protected readonly Subject<ReactorMessage> _messages = new();
    protected readonly List<EventsToObserve> _deliveries = [];
    protected ReactorEventsObserver _deliver;
    IDisposable _subscription;

    async Task Establish()
    {
        var subscribed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mediator = Substitute.For<IReactorMediator>();
        mediator.When(_ => _.Subscribe(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReactorEventsObserver>()))
            .Do(call =>
            {
                _deliver = call.Arg<ReactorEventsObserver>();
                subscribed.TrySetResult();
            });
        var reactors = new Reactors(
            Substitute.For<IGrainFactory>(),
            CreateLocalSiloDetails(),
            mediator,
            Substitute.For<Cratis.Chronicle.Storage.IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reactors>.Instance);
        _subscription = reactors.Observe(_messages).Subscribe(_deliveries.Add);
        _messages.OnNext(new(new(new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        await subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    static ILocalSiloDetails CreateLocalSiloDetails()
    {
        var details = Substitute.For<ILocalSiloDetails>();
        details.SiloAddress.Returns(SiloAddress.Zero);
        return details;
    }

    protected void Reply(ulong deliveryId = 0) => _messages.OnNext(new(new(new ReactorResult
    {
        Partition = "partition",
        DeliveryId = deliveryId,
        State = ObservationState.Success,
        LastSuccessfulObservation = 12,
        ExceptionStackTrace = string.Empty
    })));

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }
}
