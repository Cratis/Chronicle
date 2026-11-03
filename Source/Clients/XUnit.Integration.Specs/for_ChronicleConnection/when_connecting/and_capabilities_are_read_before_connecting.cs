// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.when_connecting;

public class and_capabilities_are_read_before_connecting : an_in_process_connection
{
    bool _known;

    void Because() => _known = _capabilities.CapabilitiesAreKnown;

    [Fact] void should_connect_and_know_the_capabilities() => _known.ShouldBeTrue();
    [Fact] void should_advertise_conjunctions() => _capabilities.Capabilities.ShouldContain(KernelCapabilities.ConcurrencyScopeConjunctions);
    [Fact] void should_know_the_capabilities_inside_the_connected_callback() => _knownInCallback.ShouldBeTrue();
}
