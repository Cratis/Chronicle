// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Cratis.Chronicle.Integration.for_Adapters.given;

public class one_adapter : all_dependencies
{
    protected Adapters _adapters;
    protected IAdapterFor<Model, ExternalModel> _adapter;
    protected IAdapterProjectionFor<Model> _adapterProjection;
    protected IMapper _mapper;

    void Establish()
    {
        _adapter = Substitute.For<IAdapterFor<Model, ExternalModel>>();
        var adapterType = _adapter.GetType();
        _clientArtifacts.Adapters.Returns([adapterType]);
        _serviceProvider.GetService(adapterType).Returns(_adapter);

        _mapper = Substitute.For<IMapper>();
        _mapperFactory.CreateFor(_adapter).Returns(_mapper);

        _adapterProjection = Substitute.For<IAdapterProjectionFor<Model>>();
        _projectionFactory.CreateFor(_adapter).Returns(Task.FromResult(_adapterProjection));

        _adapters = new(
            _clientArtifacts,
            _serviceProvider,
            _projectionFactory,
            _mapperFactory);
    }
}
