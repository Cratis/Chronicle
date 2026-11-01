// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Security.for_ChronicleDataProtection;

public class when_a_warmup_self_binding_already_exists : Specification
{
    ServiceCollection _services;
    ServiceDescriptor _framework;
    Exception _exception;

    void Establish()
    {
        _services = new();
        _services.AddDataProtection();
        _framework = _services.Single(_ => _.ServiceType == typeof(IHostedService));
        _services.AddTransient<DataProtectionKeyRingWarmup>();
    }

    void Because() => _exception = Catch.Exception(() => _services.DeferChronicleDataProtectionWarmup());

    [Fact] void should_reject_the_unowned_registration() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_framework_descriptor() => _services.ShouldContain(_framework);
}
