// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_the_generation_is_lower : Specification
{
    DeploymentState _stored;
    DeploymentDecision _result;

    void Establish() => _stored = new(10UL, "previous");
    void Because() => _result = DeploymentDecision.Decide(_stored, 9UL, "older");

    [Fact] void should_reject() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_the_stored_generation() => _result.Stored.ShouldEqual(_stored);
    [Fact] void should_not_record() => _result.ToRecord.ShouldBeNull();
}
