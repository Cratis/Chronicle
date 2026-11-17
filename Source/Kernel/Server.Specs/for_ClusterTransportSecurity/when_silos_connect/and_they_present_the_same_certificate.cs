// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.when_silos_connect;

public class and_they_present_the_same_certificate : given.two_silos
{
    int _active;

    async Task Because()
    {
        await _first.StartAsync();
        _second = CreateSilo(_secondPort, _secondGatewayPort, _firstCertificate);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _second.StartAsync(cts.Token);
        _active = await WaitForActiveSilos(2, TimeSpan.FromSeconds(30));
    }

    [Fact] void should_form_one_cluster_of_both_silos() => _active.ShouldEqual(2);
}
