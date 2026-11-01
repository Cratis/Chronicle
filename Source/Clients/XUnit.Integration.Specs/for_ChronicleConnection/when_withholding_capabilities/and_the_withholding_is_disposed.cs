// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.when_withholding_capabilities;

public class and_the_withholding_is_disposed : an_in_process_connection
{
    async Task Because()
    {
        await ((IChronicleConnection)_connection).Connect();
        _connection.WithholdCapabilities(KernelCapabilities.ConcurrencyScopeConjunctions).Dispose();
    }

    [Fact] void should_advertise_conjunctions_again() => _capabilities.Capabilities.ShouldContain(KernelCapabilities.ConcurrencyScopeConjunctions);
}
