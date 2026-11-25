// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.when_silos_connect;

public class and_they_present_different_certificates : given.two_silos
{
    int _active;

    async Task Because()
    {
        await _first.StartAsync();
        using var other = SelfSigned("CN=someone-else");
        _second = CreateSilo(_secondPort, _secondGatewayPort, other);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await Catch.Exception(() => _second.StartAsync(cts.Token));
        _active = await WaitForActiveSilos(2, TimeSpan.FromSeconds(1));
    }

    [Fact] void should_not_admit_the_second_silo() => _active.ShouldEqual(1);
}
