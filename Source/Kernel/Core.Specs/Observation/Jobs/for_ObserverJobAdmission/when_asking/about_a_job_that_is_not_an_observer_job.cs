// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_asking;

public class about_a_job_that_is_not_an_observer_job : given.rules
{
    ObserverJobAdmission _admission;
    bool _result;

    void Establish() => _admission = new(_storage, [Rule(100, false)]);
    async Task Because() => _result = await _admission.MayRun(AdmissionFor(Substitute.For<IJobRequest>()), CancellationToken.None);

    [Fact] void should_admit_the_job() => _result.ShouldBeTrue();
    [Fact] void should_not_ask_any_rule() => _asked.ShouldBeEmpty();
}
