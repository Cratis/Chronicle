// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering.given;

public abstract class an_invalid_index_registration : all_dependencies_with_configured_sink
{
    protected Exception _exception;
    protected Contracts.ReadModels.IReadModels _service;
    protected abstract Type ReadModelType { get; }

    void Establish()
    {
        var handler = Substitute.For<IProjectionHandler>();
        handler.ReadModelType.Returns(ReadModelType);
        handler.Id.Returns(new ProjectionId("invalid-index"));
        _projections.GetAllHandlers().Returns([handler]);
        _reducers.GetAllHandlers().Returns([]);
        _service = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_service);
    }
}
