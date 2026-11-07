// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_registration_attempt : Specification
{
    protected ConnectionOwnership _ownership;
    protected IReducerMediator _mediator;
    protected IGrainFactory _grainFactory;
    protected IReadModel _definitionGrain;
    protected Exception _error;

    void Establish()
    {
        _ownership = new();
        _mediator = Substitute.For<IReducerMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _definitionGrain = Substitute.For<IReadModel>();
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_definitionGrain);
        _grainFactory.ClearReceivedCalls();
    }

    protected async Task Register(CallContext context)
    {
        using var messages = new Subject<ReducerMessage>();
        var reducers = new Reducers(
            _grainFactory, _mediator, Substitute.For<IExpandoObjectConverter>(), new JsonSerializerOptions(), new ActivitySource<Reducers>(), Options.Create(new ChronicleOptions()), _ownership, NullLogger<Reducers>.Instance, Substitute.For<IObserverRegistrationGate>(), NamespaceCasingContext.Create());
        var observed = reducers.Observe(messages, context).ToTask();
        messages.OnNext(new ReducerMessage(new Contracts.Primitives.OneOf<RegisterReducer, ReducerResult>(new RegisterReducer
        {
            ConnectionId = "connection",
            EventStore = "application",
            Namespace = "namespace",
            Reducer = new ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = "read-model" }
        })));
        _error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
