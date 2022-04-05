// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_Adapters;

public class when_getting_projection_for_known_model_and_external_model : given.one_adapter
{
    IAdapterProjectionFor<Model> result;

    void Because() => result = adapters.GetProjectionFor<Model, ExternalModel>();

    [Fact] void should_return_expected_projection() => result.ShouldEqual(adapter_projection.Object);
}
