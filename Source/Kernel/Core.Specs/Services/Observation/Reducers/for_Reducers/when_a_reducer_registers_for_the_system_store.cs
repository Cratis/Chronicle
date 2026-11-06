// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_a_reducer_registers_for_the_system_store : Specification
{
    [Theory]
    [InlineData("SYSTEM", "read-model", StatusCode.PermissionDenied)]
    [InlineData("application", "x#System", StatusCode.InvalidArgument)]
    public async Task should_refuse_before_resolving_the_definition_or_subscribing(string store, string readModel, StatusCode status)
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var definitionGrain = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(definitionGrain);
        grainFactory.ClearReceivedCalls();
        var mediator = Substitute.For<IReducerMediator>();
        var reducers = new Reducers(
            grainFactory,
            mediator,
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reducers>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reducers>.Instance,
            Substitute.For<IObserverRegistrationGate>(),
            NamespaceCasingContext.Create());
        using var messages = new Subject<ReducerMessage>();
        var observed = reducers.Observe(messages).ToTask();

        messages.OnNext(new ReducerMessage(new Contracts.Primitives.OneOf<RegisterReducer, ReducerResult>(new RegisterReducer
        {
            ConnectionId = "connection",
            EventStore = store,
            Namespace = "namespace",
            Reducer = new ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = readModel }
        })));
        var error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5)));

        (error is RpcException rpc && rpc.StatusCode == status).ShouldBeTrue();
        grainFactory.ReceivedCalls().ShouldBeEmpty();
        definitionGrain.ReceivedCalls().ShouldBeEmpty();
        mediator.ReceivedCalls().ShouldBeEmpty();
    }
}
