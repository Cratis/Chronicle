// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Cratis.Chronicle.Integration.for_AdapterMapperFactory;

public class when_creating_for_adapter : Specification
{
    IAdapterFor<Model, ExternalModel> _adapter;

    AdapterMapperFactory _factory;
    IMapper _result;

    void Establish()
    {
        _factory = new();
        _adapter = Substitute.For<IAdapterFor<Model, ExternalModel>>();
    }

    void Because() => _result = _factory.CreateFor(_adapter);

    [Fact] void should_define_model() => _adapter.Received(1).DefineImportMapping(Arg.Any<IMappingExpression<ExternalModel, Model>>());
    [Fact] void should_return_mapper() => _result.ShouldNotBeNull();
}
