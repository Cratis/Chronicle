// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRevisionAcknowledgementsTally;

public class when_tallying_a_deleted_definition : Specification
{
    static readonly SiloAddress _applied = SiloAddress.New(IPAddress.Loopback, 11111, 1);
    static readonly SiloAddress _stillHolding = SiloAddress.New(IPAddress.Loopback, 11112, 1);

    ObserverRevisionAcknowledgements _result;

    void Because() => _result = ObserverRevisionAcknowledgementsTally.Tally(
        ObserverDefinitionRevision.None,
        [(_applied, ObserverDefinitionRevision.None), (_stillHolding, new ObserverDefinitionRevision(3))]);

    [Fact] void should_count_only_the_silo_holding_no_definition() => _result.Acknowledged.ShouldEqual(1);
    [Fact] void should_report_the_silo_still_holding_it_as_lagging() => _result.Lagging.ShouldContainOnly(_stillHolding);
}
