// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Cratis.Chronicle.Integration.for_Adapters;

public class when_getting_mapper_for_known_model_and_external_model : given.one_adapter
{
    IMapper _result;

    void Because() => _result = _adapters.GetMapperFor<Model, ExternalModel>();

    [Fact] void should_return_expected_mapper() => _result.ShouldEqual(_mapper);
}
