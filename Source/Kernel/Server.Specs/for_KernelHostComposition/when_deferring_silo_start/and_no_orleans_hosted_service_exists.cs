// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_deferring_silo_start;

public class and_no_orleans_hosted_service_exists : Specification
{
    IServiceCollection _services;
    Exception? _error;

    void Establish()
    {
        _services = new ServiceCollection();
    }

    void Because() => _error = Catch.Exception(_services.UseDeferredSiloStart);

    [Fact] void should_fail_loudly() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
