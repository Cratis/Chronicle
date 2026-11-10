// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_BusySilos.when_finding_a_busy_silo;

public class and_the_call_timed_out_against_this_silo : given.a_cluster
{
    void Because() => _result = _busySilos.FindBusySilo(TimeoutTo(_thisSilo.ToString()));

    [Fact] void should_find_a_busy_silo() => _result.ShouldNotBeNull();
    [Fact] void should_be_this_silo() => _result!.IsThisSilo.ShouldBeTrue();
    [Fact] void should_name_the_silo_as_the_call_did() => ("S" + _result!.Address).ShouldEqual(_thisSilo.ToString());
}
