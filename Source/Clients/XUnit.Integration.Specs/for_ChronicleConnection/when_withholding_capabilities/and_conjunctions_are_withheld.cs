// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.when_withholding_capabilities;

public class and_conjunctions_are_withheld : an_in_process_connection
{
    string[] _advertised;
    IDisposable _withholding;

    async Task Because()
    {
        await ((IChronicleConnection)_connection).Connect();
        _advertised = [.. _capabilities.Capabilities];
        _withholding = _connection.WithholdCapabilities(KernelCapabilities.ConcurrencyScopeConjunctions);
        await _connection.Reconnect();
    }

    void Destroy() => _withholding.Dispose();

    [Fact] void should_not_advertise_conjunctions() => _capabilities.Capabilities.ShouldNotContain(KernelCapabilities.ConcurrencyScopeConjunctions);
    [Fact] void should_still_advertise_the_other_capabilities() => _capabilities.Capabilities.ShouldEqual(_advertised.Where(_ => _ != KernelCapabilities.ConcurrencyScopeConjunctions));
    [Fact] void should_keep_conjunctions_withheld_inside_the_reconnected_callback() => _capabilitiesInCallback.ShouldNotContain(KernelCapabilities.ConcurrencyScopeConjunctions);
    [Fact] void should_still_know_the_capabilities() => _capabilities.CapabilitiesAreKnown.ShouldBeTrue();
}
