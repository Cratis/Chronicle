// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Adapters;

public class when_getting_adapter_for_known_model_and_external_model : given.one_adapter
{
    IAdapterFor<Model, ExternalModel> _result;

    void Because() => _result = _adapters.GetFor<Model, ExternalModel>();

    [Fact] void should_return_expected_adapter() => _result.ShouldEqual(_adapter);
}
