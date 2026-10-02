// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_clearing_the_quarantine_of_an_unknown_partition : Specification
{
    FailedPartitions _failedPartitions;
    bool _result;

    void Establish() => _failedPartitions = new();

    void Because() => _result = _failedPartitions.ClearQuarantine(new Key("unknown", Properties.ArrayIndexers.NoIndexers));

    [Fact] void should_report_that_nothing_changed() => _result.ShouldBeFalse();
}
