// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration.for_Adapters;

public class when_getting_mapper_for_known_model_and_external_model : given.one_adapter
{
    IMapper result;

    void Because() => result = adapters.GetMapperFor<Model, ExternalModel>();

    [Fact] void should_return_expected_mapper() => result.ShouldEqual(mapper.Object);
}
