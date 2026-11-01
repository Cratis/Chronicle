// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.when_connecting;

public class and_the_kernel_supports_conjunctions : an_in_process_connection
{
    CompatibilityResponse _kernel;

    async Task Because()
    {
        await ((IChronicleConnection)_connection).Connect();
        _kernel = await CheckKernelCompatibility();
    }

    [Fact] void should_know_the_capabilities() => _capabilities.CapabilitiesAreKnown.ShouldBeTrue();
    [Fact] void should_advertise_conjunctions() => _capabilities.Capabilities.ShouldContain(KernelCapabilities.ConcurrencyScopeConjunctions);
    [Fact] void should_know_the_capabilities_inside_the_connected_callback() => _knownInCallback.ShouldBeTrue();
    [Fact] void should_advertise_conjunctions_inside_the_connected_callback() => _capabilitiesInCallback.ShouldContain(KernelCapabilities.ConcurrencyScopeConjunctions);
    [Fact] void should_advertise_exactly_what_the_kernel_reports() => _capabilities.Capabilities.OrderBy(_ => _).ShouldEqual(_kernel.Capabilities.OrderBy(_ => _));
}
