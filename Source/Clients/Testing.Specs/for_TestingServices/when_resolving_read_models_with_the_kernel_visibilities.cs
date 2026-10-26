// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_resolving_read_models_with_the_kernel_visibilities : given.testing_services
{
    IReadModels _result;

    void Because() => _result = _services.ReadModels;

    [Fact] void should_compose_the_kernel_read_models_service() => _result.ShouldNotBeNull();
}
