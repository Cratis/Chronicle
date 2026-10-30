// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering.given;

public class an_indexed_read_model : all_dependencies_with_configured_sink
{
    [CompositeIndex(nameof(Region), nameof(Created), IsDescending = new[] { false, true })]
    [CompositeIndex("Address.City", nameof(Region))]
    protected record MyReadModel(Guid Id, string Region, int Created, Location Address, [property: Index(IsDescending = true)] int Total, [Index(IsDescending = true)] int Sequence, [Index] string Legacy);
    protected record Location([property: Index] string City);

    protected RegisterManyRequest _request;
    protected Contracts.ReadModels.IReadModels _service;

    void Establish()
    {
        var handler = Substitute.For<IProjectionHandler>();
        handler.ReadModelType.Returns(typeof(MyReadModel));
        handler.Id.Returns(new ProjectionId("ordered-indexes"));
        _projections.GetAllHandlers().Returns([handler]);
        _reducers.GetAllHandlers().Returns([]);
        _service = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_service);
        _service.When(service => service.RegisterMany(Arg.Any<RegisterManyRequest>())).Do(call => _request = call.Arg<RegisterManyRequest>());
    }
}
