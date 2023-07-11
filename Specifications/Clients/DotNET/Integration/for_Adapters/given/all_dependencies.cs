// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_Adapters.given;

public class all_dependencies : Specification
{
    protected Mock<IClientArtifactsProvider> client_artifacts;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IAdapterProjectionFactory> projection_factory;
    protected Mock<IAdapterMapperFactory> mapper_factory;

    void Establish()
    {
        client_artifacts = new();
        service_provider = new();
        projection_factory = new();
        mapper_factory = new();
    }
}
