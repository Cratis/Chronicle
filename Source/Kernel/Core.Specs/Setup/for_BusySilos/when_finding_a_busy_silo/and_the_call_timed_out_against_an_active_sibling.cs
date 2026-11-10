// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_BusySilos.when_finding_a_busy_silo;

public class and_the_call_timed_out_against_an_active_sibling : given.a_cluster
{
    void Because() => _result = _busySilos.FindBusySilo(TimeoutTo(_activeSibling.ToString()));

    [Fact] void should_find_a_busy_silo() => _result.ShouldNotBeNull();
    [Fact] void should_be_a_sibling() => _result!.IsThisSilo.ShouldBeFalse();
}
