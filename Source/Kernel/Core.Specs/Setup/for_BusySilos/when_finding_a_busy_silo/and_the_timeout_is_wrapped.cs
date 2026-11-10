// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_BusySilos.when_finding_a_busy_silo;

public class and_the_timeout_is_wrapped : given.a_cluster
{
    void Because() => _result = _busySilos.FindBusySilo(
        new AggregateException(new InvalidOperationException("other"), new InvalidOperationException("wrapper", TimeoutTo(_thisSilo.ToString()))));

    [Fact] void should_find_this_silo() => _result!.IsThisSilo.ShouldBeTrue();
}
