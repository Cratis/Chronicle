// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Adapters.given;

public class all_dependencies : Specification
{
    protected IClientArtifactsProvider _clientArtifacts;
    protected IServiceProvider _serviceProvider;
    protected IAdapterProjectionFactory _projectionFactory;
    protected IAdapterMapperFactory _mapperFactory;

    void Establish()
    {
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _projectionFactory = Substitute.For<IAdapterProjectionFactory>();
        _mapperFactory = Substitute.For<IAdapterMapperFactory>();
    }
}
