// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_Importer;

public class when_getting_for_model_and_external_model : Specification
{
    IAdapters _adapters;
    IEventLog _eventLog;
    IAdapterFor<Model, ExternalModel> _adapter;
    IAdapterProjectionFor<Model> _projection;
    IMapper _mapper;
    IObjectComparer _objectComparer;
    ICausationManager _causationManager;
    Importer _importer;
    IImportOperations<Model, ExternalModel> _operations;

    void Establish()
    {
        _adapters = Substitute.For<IAdapters>();
        _eventLog = Substitute.For<IEventLog>();
        _adapter = Substitute.For<IAdapterFor<Model, ExternalModel>>();
        _projection = Substitute.For<IAdapterProjectionFor<Model>>();
        _mapper = Substitute.For<IMapper>();
        _causationManager = Substitute.For<ICausationManager>();

        _adapters.GetFor<Model, ExternalModel>().Returns(_adapter);
        _adapters.GetProjectionFor<Model, ExternalModel>().Returns(_projection);
        _adapters.GetMapperFor<Model, ExternalModel>().Returns(_mapper);

        _objectComparer = Substitute.For<IObjectComparer>();
        _importer = new(_adapters, _objectComparer, _eventLog, _causationManager);
    }

    void Because() => _operations = _importer.For<Model, ExternalModel>();

    [Fact] void should_get_operations() => _operations.ShouldNotBeNull();
    [Fact] void should_hold_correct_adapter() => _operations.Adapter.ShouldEqual(_adapter);
    [Fact] void should_hold_correct_projection() => _operations.Projection.ShouldEqual(_projection);
    [Fact] void should_hold_correct_mapper() => _operations.Mapper.ShouldEqual(_mapper);
}
