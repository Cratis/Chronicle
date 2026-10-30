// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_an_unchecked_fingerprint_is_unchanged : Specification
{
    DeploymentDecision _result;

    void Because() => _result = DeploymentDecision.Decide(new(10UL, "same"), 0UL, "same");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_record() => _result.ToRecord.ShouldBeNull();
}
