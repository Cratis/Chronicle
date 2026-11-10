// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication;
using Cratis.Chronicle.Setup;

namespace Cratis.Chronicle.Server.for_KernelHostedServices;

/// <summary>
/// Hosted services start before the listeners bind, and the silo is deferred until after they have all started. A
/// hosted service that calls a grain therefore waits for a silo that cannot start until it returns, and the port never
/// binds (#4040). Work that needs grains belongs to the silo lifecycle instead.
/// </summary>
public class when_composing_the_kernel_services : given.the_kernel_services
{
    Type[] _hostedServices;
    string[] _offenders;

    void Because()
    {
        _hostedServices = [.. HostedServiceGrainAccess.RegisteredHostedServices(_services)];
        _offenders = [.. HostedServiceGrainAccess.Offenders(_services)];
    }

    [Fact] void should_inspect_the_kernel_hosted_services() => _hostedServices.ShouldContain(typeof(AdministratorPolicyStartupWarning));
    [Fact] void should_inspect_the_deferred_silo() => _hostedServices.ShouldContain(typeof(KernelHostComposition));
    [Fact] void should_have_no_hosted_service_that_reaches_grains() => _offenders.ShouldBeEmpty();
    [Fact] void should_tell_busy_silos_apart_for_the_startup_task() => _services.Any(_ => _.ServiceType == typeof(IBusySilos)).ShouldBeTrue();
}
