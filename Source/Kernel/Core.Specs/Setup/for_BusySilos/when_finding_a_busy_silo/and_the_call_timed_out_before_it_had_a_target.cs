// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_BusySilos.when_finding_a_busy_silo;

public class and_the_call_timed_out_before_it_had_a_target : given.a_cluster
{
    void Because() => _result = _busySilos.FindBusySilo(TimeoutTo(null));

    [Fact] void should_not_find_a_busy_silo() => _result.ShouldBeNull();
}
