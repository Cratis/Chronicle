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

public class a_registration_attempt : Specification
{
    protected ConnectionOwnership _ownership;
    protected IReactorMediator _mediator;
    protected IGrainFactory _grainFactory;
    protected Exception _error;

    void Establish()
    {
        _ownership = new();
        _mediator = Substitute.For<IReactorMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
    }

    protected async Task Register(CallContext context)
    {
        using var messages = new Subject<ReactorMessage>();
        var reactors = new Reactors(
            _grainFactory, _mediator, Substitute.For<IStorage>(), new JsonSerializerOptions(), new ActivitySource<Reactors>(), Options.Create(new ChronicleOptions()), _ownership, NullLogger<Reactors>.Instance, Substitute.For<IObserverRegistrationGate>());
        var observed = reactors.Observe(messages, context).ToTask();
        messages.OnNext(new ReactorMessage(new Contracts.Primitives.OneOf<RegisterReactor, ReactorResult>(new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        _error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
