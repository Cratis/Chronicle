// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_registering_definitions_with_separators : given.all_dependencies
{
    [Theory]
    [InlineData("many", false)]
    [InlineData("single", false)]
    [InlineData("update", false)]
    [InlineData("many", true)]
    [InlineData("single", true)]
    [InlineData("update", true)]
    public async Task should_refuse_before_any_grain_or_storage_access(string operation, bool observer)
    {
        _grainFactory.ClearReceivedCalls();
        _storage.ClearReceivedCalls();
        var definition = new Contracts.ReadModels.ReadModelDefinition
        {
            Type = new ReadModelType { Identifier = observer ? "read-model" : "x#System" },
            ObserverIdentifier = observer ? "x#System" : "observer"
        };
        var error = await Catch.Exception(() => operation switch
        {
            "many" => _service.RegisterMany(new RegisterManyRequest { EventStore = "application", ReadModels = [definition] }),
            "single" => _service.RegisterSingle(new RegisterSingleRequest { EventStore = "application", ReadModel = definition }),
            _ => _service.UpdateDefinition(new UpdateDefinitionRequest { EventStore = "application", ReadModel = definition })
        });

        (error is RpcException { StatusCode: StatusCode.InvalidArgument }).ShouldBeTrue();
        _grainFactory.ReceivedCalls().ShouldBeEmpty();
        _readModel.ReceivedCalls().ShouldBeEmpty();
        _storage.ReceivedCalls().ShouldBeEmpty();
    }
}
