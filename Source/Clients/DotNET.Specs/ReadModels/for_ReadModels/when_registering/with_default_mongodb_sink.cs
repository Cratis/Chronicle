// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_default_mongodb_sink : given.all_dependencies_with_configured_sink
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    Contracts.ReadModels.IReadModels _readModelsService;
    RegisterManyRequest _capturedRequest;
    IProjectionHandler _projectionHandler;

    void Establish()
    {
        _projectionHandler = Substitute.For<IProjectionHandler>();
        _projectionHandler.ReadModelType.Returns(typeof(MyReadModel));
        _projectionHandler.Id.Returns(new ProjectionId(Guid.NewGuid().ToString()));

        _projections.GetAllHandlers().Returns([_projectionHandler]);
        _reducers.GetAllHandlers().Returns([]);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.When(_ => _.RegisterMany(Arg.Any<RegisterManyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<RegisterManyRequest>());
    }

    async Task Because() => await _readModels.Register();

    [Fact] void should_register_with_mongodb_sink_type() => _capturedRequest.ReadModels[0].Sink.TypeId.ShouldEqual(WellKnownSinkTypes.MongoDB.Value);
}
