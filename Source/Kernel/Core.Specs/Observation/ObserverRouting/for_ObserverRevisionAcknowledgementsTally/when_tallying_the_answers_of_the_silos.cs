// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRevisionAcknowledgementsTally;

public class when_tallying_the_answers_of_the_silos : Specification
{
    static readonly SiloAddress _current = SiloAddress.New(IPAddress.Loopback, 11111, 1);
    static readonly SiloAddress _ahead = SiloAddress.New(IPAddress.Loopback, 11112, 1);
    static readonly SiloAddress _behind = SiloAddress.New(IPAddress.Loopback, 11113, 1);
    static readonly SiloAddress _silent = SiloAddress.New(IPAddress.Loopback, 11114, 1);

    ObserverRevisionAcknowledgements _result;

    void Because() => _result = ObserverRevisionAcknowledgementsTally.Tally(
        new ObserverDefinitionRevision(5),
        [(_current, new ObserverDefinitionRevision(5)), (_ahead, new ObserverDefinitionRevision(6)), (_behind, new ObserverDefinitionRevision(4)), (_silent, null)]);

    [Fact] void should_count_the_silos_holding_the_revision_or_a_later_one() => _result.Acknowledged.ShouldEqual(2);
    [Fact] void should_report_the_silos_behind_and_the_ones_that_did_not_answer_as_lagging() => _result.Lagging.ShouldContainOnly(_behind, _silent);
    [Fact] void should_not_be_complete() => _result.IsComplete.ShouldBeFalse();
}
