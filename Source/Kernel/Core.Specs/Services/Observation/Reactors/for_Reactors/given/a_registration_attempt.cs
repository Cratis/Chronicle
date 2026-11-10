// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class a_registration_attempt : Observation.given.a_connection_held_on_another_silo
{
    protected ConnectionOwnership _ownership;
    protected IReactorMediator _mediator;
    protected IGrainFactory _grainFactory;
    protected Exception _error;
    protected IConnectionReservation _reservation;
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _ownership = new();
        _mediator = Substitute.For<IReactorMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _reservation = ReservationForCheck();
        _grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reservation);
        var client = Substitute.For<IReactor>();
        _grainFactory.GetGrain<IReactor>(Arg.Any<string>()).Returns(client);
        client.SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reactors.ReactorDefinition>()).Returns(_ =>
        {
            _subscribed.TrySetResult();
            return Task.CompletedTask;
        });
        _grainFactory.ClearReceivedCalls();
    }

    protected virtual IConnectionReservation ReservationForCheck() => Substitute.For<IConnectionReservation>();

    protected async Task Register(CallContext context, bool expectRegistration = false)
    {
        using var messages = new Subject<ReactorMessage>();
        var reactors = new Reactors(
            _grainFactory, _mediator, Substitute.For<IStorage>(), new JsonSerializerOptions(), new ActivitySource<Reactors>(), Options.Create(new ChronicleOptions { Clustering = new() { ResponseTimeout = TimeSpan.FromMilliseconds(100) } }), _ownership, NullLogger<Reactors>.Instance, Substitute.For<IObserverRegistrationGate>(), NamespaceCasingContext.Create());
        using var cancellation = new CancellationTokenSource();
        var observed = reactors.Observe(messages, context).ToTask(cancellation.Token);
        messages.OnNext(new ReactorMessage(new Contracts.Primitives.OneOf<RegisterReactor, ReactorResult>(new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        if (expectRegistration)
        {
            _error = await Catch.Exception(() => _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), _clock));
            await cancellation.CancelAsync();
            await Catch.Exception(() => observed);
        }
        else
        {
            _error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5), _clock));
        }
    }
}
