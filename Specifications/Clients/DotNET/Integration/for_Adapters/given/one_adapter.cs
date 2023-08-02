// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration.for_Adapters.given;

public class one_adapter : all_dependencies
{
    protected Adapters adapters;
    protected Mock<IAdapterFor<Model, ExternalModel>> adapter;

    protected Mock<IAdapterProjectionFor<Model>> adapter_projection;
    protected Mock<IMapper> mapper;

    void Establish()
    {
        adapter = new Mock<IAdapterFor<Model, ExternalModel>>();
        var adapterType = adapter.Object.GetType();
        client_artifacts.SetupGet(_ => _.Adapters).Returns(new[] { adapterType });
        service_provider.Setup(_ => _.GetService(adapterType)).Returns(adapter.Object);

        mapper = new();
        mapper_factory.Setup(_ => _.CreateFor(adapter.Object)).Returns(mapper.Object);

        adapter_projection = new();
        projection_factory.Setup(_ => _.CreateFor(adapter.Object)).Returns(Task.FromResult(adapter_projection.Object));

        adapters = new(
            client_artifacts.Object,
            service_provider.Object,
            projection_factory.Object,
            mapper_factory.Object);
    }
}
