// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration.for_AdapterMapperFactory;

public class when_creating_for_adapter : Specification
{
    Mock<IAdapterFor<Model, ExternalModel>> adapter;

    AdapterMapperFactory factory;
    IMapper result;

    void Establish()
    {
        factory = new();
        adapter = new();
    }

    void Because() => result = factory.CreateFor(adapter.Object);

    [Fact] void should_define_model() => adapter.Verify(_ => _.DefineImportMapping(IsAny<IMappingExpression<ExternalModel, Model>>()), Once);
    [Fact] void should_return_mapper() => result.ShouldNotBeNull();
}
