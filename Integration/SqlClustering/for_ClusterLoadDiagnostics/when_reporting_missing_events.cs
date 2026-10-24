// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_reporting_missing_events : Specification
{
    readonly Guid _id = Guid.NewGuid();
    string _description = null!;

    void Because() => _description = ClusterLoadDiagnostics.DescribeMissingSequences(
        [(_id, (EventSequenceNumber)2), (_id, (EventSequenceNumber)3), (_id, (EventSequenceNumber)4)],
        [(_id, (EventSequenceNumber)2), (_id, (EventSequenceNumber)2), (Guid.NewGuid(), (EventSequenceNumber)3)]);

    [Fact] public void should_identify_missing_events_by_both_payload_and_sequence() => _description.ShouldEqual("missing stored event sequence numbers (2, first 50): 3, 4");
}
