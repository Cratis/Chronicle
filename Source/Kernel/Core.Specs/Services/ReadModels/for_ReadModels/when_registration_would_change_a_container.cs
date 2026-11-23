// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_registration_would_change_a_container : given.all_dependencies
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_return_the_actionable_failed_precondition(bool many)
    {
        var manager = Substitute.For<IReadModelsManager>();
        _grainFactory.GetGrain<IReadModelsManager>(Arg.Any<string>()).Returns(manager);
        var refusal = new ReadModelContainerChangeRequiresExplicitUpdate("model", "stored-container", "requested-container");
        manager.Register(Arg.Any<IEnumerable<Chronicle.Concepts.ReadModels.ReadModelDefinition>>()).Returns(Task.FromException(refusal));
        manager.RegisterSingle(Arg.Any<Chronicle.Concepts.ReadModels.ReadModelDefinition>()).Returns(Task.FromException(refusal));
        var definition = _readModelDefinition.ToContract();

        var error = await Catch.Exception(() => many
            ? _service.RegisterMany(new RegisterManyRequest { EventStore = "application", ReadModels = [definition] })
            : _service.RegisterSingle(new RegisterSingleRequest { EventStore = "application", ReadModel = definition }));

        error.ShouldBeOfExactType<RpcException>();
        var rpc = (RpcException)error;
        rpc.StatusCode.ShouldEqual(StatusCode.FailedPrecondition);
        rpc.Status.Detail.ShouldEqual(refusal.Message);
        await manager.DidNotReceive().UpdateDefinition(Arg.Any<Chronicle.Concepts.ReadModels.ReadModelDefinition>());
        _storage.ReceivedCalls().ShouldBeEmpty();
    }
}
