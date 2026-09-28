// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_a_read_model_maintained_by_a_passive_projection : given.all_dependencies_with_configured_sink
{
    record Inventory(string Name);

    Contracts.ReadModels.IReadModels _readModelsService;
    RegisterManyRequest _capturedRequest;

    void Establish()
    {
        var projectionHandler = Substitute.For<IProjectionHandler>();
        projectionHandler.ReadModelType.Returns(typeof(Inventory));
        projectionHandler.Id.Returns(new ProjectionId("inventory"));

        _projections.GetAllHandlers().Returns([projectionHandler]);
        ((IKnowPassiveProjections)_projections).IsPassive(typeof(Inventory)).Returns(true);
        _reducers.GetAllHandlers().Returns([]);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.When(_ => _.RegisterMany(Arg.Any<RegisterManyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<RegisterManyRequest>());
    }

    async Task Because() => await _readModels.Register();

    [Fact] void should_register_with_none_sink_type_so_it_is_computed_on_demand() => _capturedRequest.ReadModels[0].Sink.TypeId.ShouldEqual(SinkTypeId.None.Value);
}
