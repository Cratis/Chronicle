// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_asking;

public class and_every_rule_admits : given.rules
{
    ObserverJobAdmission _admission;
    bool _result;

    void Establish() => _admission = new(_storage, [Rule(300, true), Rule(100, true), Rule(200, true)]);
    async Task Because() => _result = await _admission.MayRun(AdmissionFor(_request), CancellationToken.None);

    [Fact] void should_admit_the_job() => _result.ShouldBeTrue();
    [Fact] void should_ask_lowest_order_first() => _asked.ShouldEqual(new List<int> { 100, 200, 300 });
}
