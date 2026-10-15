// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_claiming_a_value_concurrently : Specification
{
    EventScenario _scenario;
    AppendResult[] _results;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var value = Guid.NewGuid().ToString();
        _results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => _scenario.EventLog.Append(EventSourceId.New(), new DiscoveredHandleClaimed(value)))));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_accept_exactly_one_claim() => _results.Count(result => result.IsSuccess).ShouldEqual(1);
    [Fact] void should_reject_every_other_claim_with_the_constraint() => _results.Where(result => !result.IsSuccess).All(result => result.ConstraintViolations.Any(violation => violation.ConstraintName == DiscoveredHandleClaimed.ConstraintName)).ShouldBeTrue();
}
