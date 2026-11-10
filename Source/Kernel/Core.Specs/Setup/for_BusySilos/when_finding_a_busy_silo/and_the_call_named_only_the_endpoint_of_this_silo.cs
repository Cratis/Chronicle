// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_BusySilos.when_finding_a_busy_silo;

public class and_the_call_named_only_the_endpoint_of_this_silo : given.a_cluster
{
    void Because() => _result = _busySilos.FindBusySilo(TimeoutTo($"S{_thisSilo.Endpoint}"));

    [Fact] void should_be_this_silo() => _result!.IsThisSilo.ShouldBeTrue();
}
